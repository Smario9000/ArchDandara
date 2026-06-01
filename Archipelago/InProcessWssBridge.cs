/*
 * ArchDandara documentation
 * Purpose: Fallback websocket bridge implementation for hosted AP rooms.
 * Why: Dandara runs on an old Mono runtime with weak TLS support, so bridge logic routes local ws traffic to hosted wss rooms when needed.
 * Notes: This exists as a fallback only; the external bridge is preferred because it is not limited by Dandara old Mono APIs.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace ArchDandara.Archipelago
{
    public static class InProcessWssBridge
    {
        private const int BufferSize = 32768;
        private static readonly Dictionary<int, BridgeServer> Servers = new Dictionary<int, BridgeServer>();

        public static bool EnsureStarted(Uri target, int listenPort, out string error)
        {
            error = null;
            if (target == null)
            {
                error = "missing hosted AP target URI";
                return false;
            }

            Monitor.Enter(Servers);
            try
            {
                BridgeServer existing;
                if (Servers.TryGetValue(listenPort, out existing))
                    return existing.Target == target.ToString();

                try
                {
                    BridgeServer server = new BridgeServer(target, listenPort);
                    server.Start();
                    Servers[listenPort] = server;
                    MLLog.Msg("[APClient] Started in-process AP WSS bridge: ws://127.0.0.1:" +
                              listenPort + "/ -> " + target);
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.GetType().Name + ": " + ex.Message;
                    return false;
                }
            }
            finally
            {
                Monitor.Exit(Servers);
            }
        }

        private sealed class BridgeServer
        {
            public readonly string Target;
            private readonly Uri TargetUri;
            private readonly int ListenPort;
            private TcpListener Listener;
            private Thread ListenerThread;
            private volatile bool Running;

            public BridgeServer(Uri targetUri, int listenPort)
            {
                TargetUri = targetUri;
                Target = targetUri.ToString();
                ListenPort = listenPort;
            }

            public void Start()
            {
                Listener = new TcpListener(IPAddress.Loopback, ListenPort);
                Listener.Start();
                Running = true;
                ListenerThread = new Thread(ListenLoop);
                ListenerThread.IsBackground = true;
                ListenerThread.Name = "ArchDandara AP WSS Bridge";
                ListenerThread.Start();
            }

            private void ListenLoop()
            {
                while (Running)
                {
                    try
                    {
                        TcpClient client = Listener.AcceptTcpClient();
                        Thread clientThread = new Thread(() => HandleClient(client));
                        clientThread.IsBackground = true;
                        clientThread.Name = "ArchDandara AP WSS Bridge Client";
                        clientThread.Start();
                    }
                    catch (Exception ex)
                    {
                        if (Running)
                            MLLog.Warning("[APClient] In-process AP WSS bridge accept failed: " +
                                          ex.GetType().Name + ": " + ex.Message);
                    }
                }
            }

            private void HandleClient(TcpClient localClient)
            {
                RemoteWebSocket remote = null;
                NetworkStream localStream = null;
                object localWriteLock = new object();

                try
                {
                    localStream = localClient.GetStream();
                    if (!CompleteLocalHandshake(localStream))
                        return;

                    remote = RemoteWebSocket.Connect(TargetUri);

                    MLLog.Msg("[APClient] In-process AP WSS bridge connected local client to " + TargetUri);
                    RemoteWebSocket remoteSocket = remote;
                    NetworkStream localSocket = localStream;
                    Thread remoteThread = new Thread(() => PumpRemoteToLocal(remoteSocket, localSocket, localWriteLock));
                    remoteThread.IsBackground = true;
                    remoteThread.Name = "ArchDandara AP WSS Bridge Remote";
                    remoteThread.Start();

                    PumpLocalToRemote(localStream, remote);
                }
                catch (Exception ex)
                {
                    if (!(ex is EndOfStreamException))
                        MLLog.Warning("[APClient] In-process AP WSS bridge client closed/error: " +
                                      ex.GetType().Name + ": " + ex.Message);
                }
                finally
                {
                    if (remote != null)
                        remote.Close();
                    if (localStream != null)
                        localStream.Close();
                    localClient.Close();
                }
            }

            private static bool CompleteLocalHandshake(NetworkStream stream)
            {
                string headers = ReadHttpHeaders(stream);
                string key = GetHeader(headers, "Sec-WebSocket-Key");
                if (string.IsNullOrEmpty(key))
                    return false;

                string accept;
                using (SHA1 sha1 = SHA1.Create())
                {
                    byte[] hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(
                        key.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                    accept = Convert.ToBase64String(hash);
                }

                string response =
                    "HTTP/1.1 101 Switching Protocols\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Sec-WebSocket-Accept: " + accept + "\r\n\r\n";
                byte[] bytes = Encoding.ASCII.GetBytes(response);
                stream.Write(bytes, 0, bytes.Length);
                return true;
            }

            private static string ReadHttpHeaders(NetworkStream stream)
            {
                return ReadHttpHeadersFromStream(stream);
            }

            public static string ReadHttpHeadersFromStream(Stream stream)
            {
                List<byte> bytes = new List<byte>();
                byte[] buffer = new byte[1];
                while (true)
                {
                    int read = stream.Read(buffer, 0, 1);
                    if (read == 0)
                        throw new EndOfStreamException();

                    bytes.Add(buffer[0]);
                    int count = bytes.Count;
                    if (count >= 4 &&
                        bytes[count - 4] == '\r' &&
                        bytes[count - 3] == '\n' &&
                        bytes[count - 2] == '\r' &&
                        bytes[count - 1] == '\n')
                        break;

                    if (bytes.Count > 16384)
                        throw new InvalidOperationException("websocket headers too large");
                }

                return Encoding.ASCII.GetString(bytes.ToArray());
            }

            private static string GetHeader(string headers, string name)
            {
                string[] lines = headers.Split(new[] { "\r\n" }, StringSplitOptions.None);
                for (int i = 0; i < lines.Length; i++)
                {
                    int colon = lines[i].IndexOf(':');
                    if (colon <= 0)
                        continue;

                    string key = lines[i].Substring(0, colon).Trim();
                    if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                        return lines[i].Substring(colon + 1).Trim();
                }

                return "";
            }

            private static void PumpRemoteToLocal(RemoteWebSocket remote, NetworkStream local, object localWriteLock)
            {
                try
                {
                    while (remote.IsOpen)
                    {
                        WebSocketFrame frame = remote.ReadFrame();
                        if (frame.Opcode == 0x8)
                        {
                            WriteLocalFrameLocked(local, localWriteLock, 0x8, new byte[0], true);
                            remote.Close();
                            return;
                        }

                        if (frame.Opcode == 0x9)
                        {
                            remote.SendFrame(0xA, frame.Payload);
                            continue;
                        }

                        if (frame.Opcode == 0x1 || frame.Opcode == 0x2 || frame.Opcode == 0x0)
                            WriteLocalFrameLocked(local, localWriteLock, frame.Opcode, frame.Payload, frame.Fin);
                    }
                }
                catch
                {
                    try
                    {
                        WriteLocalFrameLocked(local, localWriteLock, 0x8, new byte[0], true);
                    }
                    catch
                    {
                    }
                }
            }

            private static void WriteLocalFrameLocked(NetworkStream local, object localWriteLock, byte opcode,
                byte[] payload, bool fin)
            {
                Monitor.Enter(localWriteLock);
                try
                {
                    WriteLocalFrame(local, opcode, payload, fin);
                }
                finally
                {
                    Monitor.Exit(localWriteLock);
                }
            }

            private static void PumpLocalToRemote(NetworkStream local, RemoteWebSocket remote)
            {
                while (remote.IsOpen)
                {
                    WebSocketFrame frame = ReadLocalFrame(local);
                    if (frame.Opcode == 0x8)
                    {
                        remote.Close();
                        return;
                    }

                    if (frame.Opcode == 0x9)
                    {
                        remote.SendFrame(0x9, frame.Payload);
                        continue;
                    }

                    if (frame.Opcode == 0x2)
                        remote.SendFrame(0x2, frame.Payload);
                    else if (frame.Opcode == 0x1 || frame.Opcode == 0x0)
                        remote.SendFrame(frame.Opcode, frame.Payload);
                }
            }

            private static WebSocketFrame ReadLocalFrame(NetworkStream stream)
            {
                return ReadFrameFromStream(stream);
            }

            public static WebSocketFrame ReadRemoteFrame(Stream stream)
            {
                return ReadFrameFromStream(stream);
            }

            private static WebSocketFrame ReadFrameFromStream(Stream stream)
            {
                byte[] header = ReadExactly(stream, 2);
                bool fin = (header[0] & 0x80) != 0;
                byte opcode = (byte)(header[0] & 0x0F);
                bool masked = (header[1] & 0x80) != 0;
                ulong length = (ulong)(header[1] & 0x7F);

                if (length == 126)
                {
                    byte[] len = ReadExactly(stream, 2);
                    length = (ulong)((len[0] << 8) | len[1]);
                }
                else if (length == 127)
                {
                    byte[] len = ReadExactly(stream, 8);
                    length = 0;
                    for (int i = 0; i < 8; i++)
                        length = (length << 8) | len[i];
                }

                byte[] mask = masked ? ReadExactly(stream, 4) : null;
                if (length > int.MaxValue)
                    throw new InvalidOperationException("websocket frame too large");

                byte[] payload = ReadExactly(stream, (int)length);
                if (masked)
                {
                    for (int i = 0; i < payload.Length; i++)
                        payload[i] = (byte)(payload[i] ^ mask[i % 4]);
                }

                return new WebSocketFrame(fin, opcode, payload);
            }

            private static byte[] ReadExactly(Stream stream, int length)
            {
                byte[] buffer = new byte[length];
                int offset = 0;
                while (offset < length)
                {
                    int read = stream.Read(buffer, offset, length - offset);
                    if (read == 0)
                        throw new EndOfStreamException();
                    offset += read;
                }

                return buffer;
            }

            private static void WriteLocalFrame(NetworkStream stream, byte opcode, byte[] payload, bool fin)
            {
                byte[] bytes = BuildLocalFrame(opcode, payload, fin);
                stream.Write(bytes, 0, bytes.Length);
            }

            private static byte[] BuildLocalFrame(byte opcode, byte[] payload, bool fin)
            {
                byte first = (byte)((fin ? 0x80 : 0x00) | opcode);
                List<byte> frame = new List<byte>();
                frame.Add(first);

                int length = payload == null ? 0 : payload.Length;
                if (length < 126)
                {
                    frame.Add((byte)length);
                }
                else if (length <= ushort.MaxValue)
                {
                    frame.Add(126);
                    frame.Add((byte)((length >> 8) & 0xFF));
                    frame.Add((byte)(length & 0xFF));
                }
                else
                {
                    frame.Add(127);
                    ulong longLength = (ulong)length;
                    for (int i = 7; i >= 0; i--)
                        frame.Add((byte)((longLength >> (8 * i)) & 0xFF));
                }

                if (payload != null)
                    frame.AddRange(payload);

                return frame.ToArray();
            }
        }

        private sealed class RemoteWebSocket
        {
            private readonly TcpClient Client;
            private readonly Stream Stream;
            private readonly object WriteLock = new object();
            private bool Open = true;

            private RemoteWebSocket(TcpClient client, Stream stream)
            {
                Client = client;
                Stream = stream;
            }

            public bool IsOpen
            {
                get { return Open; }
            }

            public static RemoteWebSocket Connect(Uri target)
            {
                TcpClient client = new TcpClient();
                client.Connect(target.Host, target.Port);
                Stream stream = client.GetStream();

                if (target.Scheme == "wss")
                {
                    stream = CreateTlsStream(stream, target.Host);
                }

                RemoteWebSocket socket = new RemoteWebSocket(client, stream);
                socket.SendHandshake(target);
                socket.ReadHandshakeResponse();
                return socket;
            }

            private static Stream CreateTlsStream(Stream baseStream, string host)
            {
                try
                {
                    SslStream ssl = new SslStream(baseStream, false, (sender, certificate, chain, errors) => true);
                    ssl.AuthenticateAsClient(host, null, SslProtocols.Tls12, false);
                    return ssl;
                }
                catch (Exception ex)
                {
                    throw new IOException("in-process TLS failed with " + ex.GetType().Name + ": " + ex.Message +
                                          ". Dandara's Mono TLS runtime cannot connect to hosted wss:// AP rooms directly.",
                        ex);
                }
            }

            public WebSocketFrame ReadFrame()
            {
                return BridgeServer.ReadRemoteFrame(Stream);
            }

            public void SendFrame(byte opcode, byte[] payload)
            {
                Monitor.Enter(WriteLock);
                try
                {
                    byte[] bytes = BuildRemoteClientFrame(opcode, payload, true);
                    Stream.Write(bytes, 0, bytes.Length);
                }
                finally
                {
                    Monitor.Exit(WriteLock);
                }
            }

            public void Close()
            {
                if (!Open)
                    return;

                Open = false;
                try
                {
                    SendFrame(0x8, new byte[0]);
                }
                catch
                {
                }

                try
                {
                    Stream.Close();
                }
                catch
                {
                }

                try
                {
                    Client.Close();
                }
                catch
                {
                }
            }

            private void SendHandshake(Uri target)
            {
                byte[] nonce = new byte[16];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                    rng.GetBytes(nonce);

                string key = Convert.ToBase64String(nonce);
                string path = string.IsNullOrEmpty(target.PathAndQuery) ? "/" : target.PathAndQuery;
                string host = target.Host + ":" + target.Port;
                string request =
                    "GET " + path + " HTTP/1.1\r\n" +
                    "Host: " + host + "\r\n" +
                    "Upgrade: websocket\r\n" +
                    "Connection: Upgrade\r\n" +
                    "Sec-WebSocket-Key: " + key + "\r\n" +
                    "Sec-WebSocket-Version: 13\r\n\r\n";
                byte[] bytes = Encoding.ASCII.GetBytes(request);
                Stream.Write(bytes, 0, bytes.Length);
            }

            private void ReadHandshakeResponse()
            {
                string headers = BridgeServer.ReadHttpHeadersFromStream(Stream);
                if (headers.IndexOf(" 101 ", StringComparison.Ordinal) < 0 &&
                    headers.IndexOf(" 101\r\n", StringComparison.Ordinal) < 0)
                    throw new IOException("remote websocket handshake failed: " + FirstHeaderLine(headers));
            }

            private static string FirstHeaderLine(string headers)
            {
                int index = headers.IndexOf("\r\n", StringComparison.Ordinal);
                return index < 0 ? headers : headers.Substring(0, index);
            }

            private static byte[] BuildRemoteClientFrame(byte opcode, byte[] payload, bool fin)
            {
                byte first = (byte)((fin ? 0x80 : 0x00) | opcode);
                List<byte> frame = new List<byte>();
                frame.Add(first);

                int length = payload == null ? 0 : payload.Length;
                if (length < 126)
                {
                    frame.Add((byte)(0x80 | length));
                }
                else if (length <= ushort.MaxValue)
                {
                    frame.Add(0xFE);
                    frame.Add((byte)((length >> 8) & 0xFF));
                    frame.Add((byte)(length & 0xFF));
                }
                else
                {
                    frame.Add(0xFF);
                    ulong longLength = (ulong)length;
                    for (int i = 7; i >= 0; i--)
                        frame.Add((byte)((longLength >> (8 * i)) & 0xFF));
                }

                byte[] mask = new byte[4];
                using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
                    rng.GetBytes(mask);
                frame.AddRange(mask);

                if (payload != null)
                {
                    for (int i = 0; i < payload.Length; i++)
                        frame.Add((byte)(payload[i] ^ mask[i % 4]));
                }

                return frame.ToArray();
            }
        }

        private sealed class WebSocketFrame
        {
            public readonly bool Fin;
            public readonly byte Opcode;
            public readonly byte[] Payload;

            public WebSocketFrame(bool fin, byte opcode, byte[] payload)
            {
                Fin = fin;
                Opcode = opcode;
                Payload = payload ?? new byte[0];
            }
        }
    }
}

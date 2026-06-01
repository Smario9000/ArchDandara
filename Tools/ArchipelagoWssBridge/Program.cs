/*
 * ArchDandara documentation
 * Purpose: External local ws-to-wss bridge for hosted Archipelago rooms.
 * Why: Dandara old Mono TLS stack cannot reliably connect to hosted secure websockets, so this helper performs TLS outside the game runtime.
 * Notes: The bridge is intentionally a small standalone process so multiple game instances can run separate local bridge ports.
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
using System.Threading.Tasks;
using WebSocketSharp;

namespace ArchipelagoWssBridge
{
    internal static class Program
    {
        private const int DefaultListenPort = 38282;
        private const int BufferSize = 32768;
        private static readonly object LogLock = new object();
        private static string LogPath;

        private static int Main(string[] args)
        {
            string target = args.Length > 0 ? args[0] : "";
            int listenPort = args.Length > 1 && int.TryParse(args[1], out int parsedPort)
                ? parsedPort
                : DefaultListenPort;
            LogPath = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
                ? args[2]
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ArchipelagoWssBridge.log");

            if (string.IsNullOrWhiteSpace(target))
            {
                Log("Usage: ArchipelagoWssBridge.exe wss://host:port/ [listenPort] [logPath]");
                return 2;
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            Log("Starting. target=" + target + " listenPort=" + listenPort + " log=" + LogPath);

            try
            {
                RunAsync(new Uri(target), listenPort).GetAwaiter().GetResult();
                return 0;
            }
            catch (Exception ex)
            {
                Log("Fatal: " + ex);
                return 1;
            }
        }

        private static async Task RunAsync(Uri target, int listenPort)
        {
            TcpListener listener = new TcpListener(IPAddress.Loopback, listenPort);
            listener.Start();
            Log("Listening on ws://127.0.0.1:" + listenPort + "/ -> " + target);

            while (true)
            {
                TcpClient localClient = await listener.AcceptTcpClientAsync().ConfigureAwait(false);
                Log("Accepted local client " + SafeEndPoint(localClient));
                _ = Task.Run(() => HandleClientAsync(localClient, target));
            }
        }

        private static async Task HandleClientAsync(TcpClient localClient, Uri target)
        {
            using (localClient)
            using (NetworkStream localStream = localClient.GetStream())
            {
                WebSocket remote = null;
                try
                {
                    localStream.ReadTimeout = 5000;
                    localStream.WriteTimeout = 5000;
                    if (!await CompleteLocalHandshakeAsync(localStream).ConfigureAwait(false))
                    {
                        Log("Local websocket handshake failed: missing Sec-WebSocket-Key");
                        return;
                    }

                    Log("Local websocket handshake completed for " + SafeEndPoint(localClient));

                    remote = new WebSocket(target.ToString());
                    remote.WaitTime = TimeSpan.FromSeconds(10);
                    remote.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;
                    remote.SslConfiguration.CheckCertificateRevocation = false;
                    remote.SslConfiguration.ServerCertificateValidationCallback =
                        (sender, certificate, chain, errors) => true;

                    object writeLock = new object();
                    remote.OnOpen += (sender, args) => Log("Remote websocket opened: " + target);
                    remote.OnError += (sender, args) =>
                        Log("Remote websocket error: " + args.Message +
                            (args.Exception == null ? "" : " | " + args.Exception.GetType().Name + ": " + args.Exception.Message));
                    remote.OnMessage += (sender, message) =>
                    {
                        byte opcode = message.IsBinary ? (byte)0x2 : (byte)0x1;
                        byte[] payload = message.IsBinary ? message.RawData : Encoding.UTF8.GetBytes(message.Data);
                        Log("Remote -> local frame opcode=" + opcode + " bytes=" + payload.Length);
                        lock (writeLock)
                            WriteLocalFrame(localStream, opcode, payload, true);
                    };
                    remote.OnClose += (sender, close) =>
                    {
                        Log("Remote websocket closed: code=" + close.Code + " reason=" + close.Reason +
                            " clean=" + close.WasClean);
                        lock (writeLock)
                            WriteLocalFrame(localStream, 0x8, Array.Empty<byte>(), true);
                    };

                    Log("Connecting remote websocket to " + target);
                    remote.Connect();
                    if (!remote.IsAlive)
                        throw new IOException("remote websocket did not open");

                    Log("Connected " + SafeEndPoint(localClient) + " -> " + target);

                    CancellationTokenSource cts = new CancellationTokenSource();
                    await PumpLocalToRemoteAsync(localStream, remote, cts).ConfigureAwait(false);
                    cts.Cancel();
                }
                catch (Exception ex)
                {
                    Log("Client closed/error: " + ex.GetType().Name + ": " + ex.Message);
                }
                finally
                {
                    Log("Closing client " + SafeEndPoint(localClient));
                    if (remote != null)
                        remote.Close();
                }
            }
        }

        private static async Task<bool> CompleteLocalHandshakeAsync(NetworkStream stream)
        {
            string headers = await ReadHttpHeadersAsync(stream).ConfigureAwait(false);
            Log("Local handshake request: " + FirstHeaderLine(headers));
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
            await stream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            return true;
        }

        private static async Task<string> ReadHttpHeadersAsync(NetworkStream stream)
        {
            List<byte> bytes = new List<byte>();
            byte[] buffer = new byte[1];
            DateTime deadline = DateTime.UtcNow.AddSeconds(5.0);
            while (true)
            {
                if (DateTime.UtcNow > deadline)
                    throw new TimeoutException("Timed out waiting for local websocket headers.");

                Task<int> readTask = stream.ReadAsync(buffer, 0, 1);
                Task completed = await Task.WhenAny(readTask, Task.Delay(500)).ConfigureAwait(false);
                if (completed != readTask)
                    continue;

                int read = readTask.Result;
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
                    throw new InvalidOperationException("Websocket headers too large.");
            }

            return Encoding.ASCII.GetString(bytes.ToArray());
        }

        private static string FirstHeaderLine(string headers)
        {
            if (string.IsNullOrEmpty(headers))
                return "<empty>";

            int end = headers.IndexOf("\r\n", StringComparison.Ordinal);
            return end < 0 ? headers : headers.Substring(0, end);
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

        private static async Task PumpLocalToRemoteAsync(NetworkStream local, WebSocket remote,
            CancellationTokenSource cts)
        {
            while (!cts.IsCancellationRequested)
            {
                WebSocketFrame frame = await ReadLocalFrameAsync(local).ConfigureAwait(false);
                Log("Local -> remote frame opcode=" + frame.Opcode + " bytes=" + frame.Payload.Length);
                if (frame.Opcode == 0x8)
                {
                    Log("Local requested close.");
                    remote.Close();
                    return;
                }

                if (frame.Opcode == 0x9)
                {
                    await WriteLocalFrameAsync(local, 0xA, frame.Payload).ConfigureAwait(false);
                    continue;
                }

                if (frame.Opcode != 0x1 && frame.Opcode != 0x2 && frame.Opcode != 0x0)
                    continue;

                if (frame.Opcode == 0x2)
                    remote.Send(frame.Payload);
                else
                    remote.Send(Encoding.UTF8.GetString(frame.Payload));
            }
        }

        private static string SafeEndPoint(TcpClient client)
        {
            try
            {
                return client != null && client.Client != null && client.Client.RemoteEndPoint != null
                    ? client.Client.RemoteEndPoint.ToString()
                    : "<unknown>";
            }
            catch
            {
                return "<unknown>";
            }
        }

        private static void Log(string message)
        {
            string line = "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] [Bridge] " + message;
            try
            {
                Console.WriteLine(line);
            }
            catch
            {
            }

            lock (LogLock)
            {
                try
                {
                    string directory = Path.GetDirectoryName(LogPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
                catch
                {
                }
            }
        }

        private static async Task<WebSocketFrame> ReadLocalFrameAsync(NetworkStream stream)
        {
            byte[] header = await ReadExactlyAsync(stream, 2).ConfigureAwait(false);
            bool fin = (header[0] & 0x80) != 0;
            byte opcode = (byte)(header[0] & 0x0F);
            bool masked = (header[1] & 0x80) != 0;
            ulong length = (ulong)(header[1] & 0x7F);

            if (length == 126)
            {
                byte[] len = await ReadExactlyAsync(stream, 2).ConfigureAwait(false);
                length = (ulong)((len[0] << 8) | len[1]);
            }
            else if (length == 127)
            {
                byte[] len = await ReadExactlyAsync(stream, 8).ConfigureAwait(false);
                length = 0;
                for (int i = 0; i < 8; i++)
                    length = (length << 8) | len[i];
            }

            byte[] mask = masked ? await ReadExactlyAsync(stream, 4).ConfigureAwait(false) : null;
            byte[] payload = await ReadExactlyAsync(stream, checked((int)length)).ConfigureAwait(false);

            if (masked)
            {
                for (int i = 0; i < payload.Length; i++)
                    payload[i] = (byte)(payload[i] ^ mask[i % 4]);
            }

            return new WebSocketFrame(fin, opcode, payload);
        }

        private static async Task<byte[]> ReadExactlyAsync(NetworkStream stream, int length)
        {
            byte[] buffer = new byte[length];
            int offset = 0;
            while (offset < length)
            {
                int read = await stream.ReadAsync(buffer, offset, length - offset).ConfigureAwait(false);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
            }

            return buffer;
        }

        private static Task WriteLocalFrameAsync(NetworkStream stream, byte opcode, byte[] payload,
            bool fin = true)
        {
            byte[] bytes = BuildLocalFrame(opcode, payload, fin);
            return stream.WriteAsync(bytes, 0, bytes.Length);
        }

        private static void WriteLocalFrame(NetworkStream stream, byte opcode, byte[] payload,
            bool fin = true)
        {
            byte[] bytes = BuildLocalFrame(opcode, payload, fin);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static byte[] BuildLocalFrame(byte opcode, byte[] payload, bool fin)
        {
            byte first = (byte)((fin ? 0x80 : 0x00) | opcode);
            List<byte> frame = new List<byte> { first };
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

        private sealed class WebSocketFrame
        {
            public readonly bool Fin;
            public readonly byte Opcode;
            public readonly byte[] Payload;

            public WebSocketFrame(bool fin, byte opcode, byte[] payload)
            {
                Fin = fin;
                Opcode = opcode;
                Payload = payload ?? Array.Empty<byte>();
            }
        }
    }
}

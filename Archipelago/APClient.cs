/*
 * ArchDandara documentation
 * Purpose: Owns the live Archipelago session, connection lifecycle, hosted bridge routing, slot-data import, hints, and synchronization.
 * Why: This is the boundary between old Unity/Mono game code and the AP server, so centralizing connection behavior keeps reconnects and save resyncs predictable.
 * Notes: Connection code runs on a background thread; any game-object work must be deferred or protected because Unity APIs are not thread-safe.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net.Sockets;
using WebSocketSharp;
using ArchDandara.Game;
using ArchDandara.Config;
using UnityEngine;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using Newtonsoft.Json.Linq;

namespace ArchDandara.Archipelago
{
    public static class APClient
    {
        public static bool Connected = false;
        public static ArchipelagoSession Session;
        private static bool Connecting;
        private static bool GoalAchievedSent;
        private static Process ExternalBridgeProcess;
        private static System.IO.FileStream ActiveConnectionLock;
        private static string ActiveConnectionLockPath;
        private static int DequeuedItemIndex;
        private static bool PendingCurrentSaveResync;
        private static bool DiagnosticProbeOpened;
        private static string DiagnosticProbeError = "";
        private static string DiagnosticProbeClose = "";
        private static string TlsValidationLogPrefix = "[APClient]";
        private static string TlsProbeHost = "";
        private const int HostedBridgeBasePort = 30000;
        private const string ExternalBridgeRelativePath = "Tools\\ArchipelagoWssBridge.exe";
        private const string ExternalBridgeLogFolderRelativePath = "Tools\\BridgeLogs";
        private static readonly Dictionary<string, APItemLocation> ItemLocationCache =
            new Dictionary<string, APItemLocation>();
        private static readonly Dictionary<string, DateTime> LastHintRequestByItem =
            new Dictionary<string, DateTime>();
        private static readonly Dictionary<string, bool> PendingFreeScoutByItem =
            new Dictionary<string, bool>();
        private const double HintCooldownSeconds = 30.0;
        private static float NextConnectionCheckTime;

        public static void Initialize()
        {
            LocationIds.Initialize();
        }

        public static void Connect()
        {
            if (Connecting)
            {
                MLLog.Msg("[APClient] Connection attempt already running.");
                return;
            }

            Connecting = true;
            System.Threading.Thread connectThread = new System.Threading.Thread(ConnectInternal);
            connectThread.IsBackground = true;
            connectThread.Name = "ArchDandara AP Connect";
            connectThread.Start();
        }

        private static void ConnectInternal()
        {
            if (Connected)
            {
                MLLog.Msg("[APClient] Already connected. Press F4 to reconnect.");
                Connecting = false;
                return;
            }

            string blockedReason;
            if (!APSaveProfileService.CanConnectToSlot(APConfig.SlotName, out blockedReason))
            {
                MLLog.Warning("[APClient] " + blockedReason);
                Connecting = false;
                return;
            }

            MLLog.Msg("[APClient] Connecting to " + APConfig.ServerAddress + ":" + APConfig.ServerPort);

            try
            {
                ServerEndpoint endpoint = ResolveServerEndpoint(APConfig.ServerAddress, APConfig.ServerPort);
                string lockError;
                // The lock is per server + slot, not global. This lets two different player slots
                // run side by side while preventing a second copy from stealing the same AP identity.
                if (!TryAcquireConnectionLock(endpoint, out lockError))
                {
                    MLLog.Warning("[APClient] Connect cancelled: " + lockError);
                    Connecting = false;
                    return;
                }

                bool usingHostedBridge = false;
                if (ShouldUseHostedBridge(endpoint))
                {
                    // Hosted AP rooms use wss://. Dandara's bundled Mono cannot reliably do that TLS
                    // handshake, so we connect the game to a local ws:// bridge instead.
                    int bridgePort = GetHostedBridgePort(endpoint);
                    if (!EnsureHostedBridge(endpoint, bridgePort))
                    {
                        StopExternalHostedBridge();
                        ReleaseConnectionLock();
                        Connecting = false;
                        return;
                    }

                    endpoint = new ServerEndpoint(new Uri("ws://127.0.0.1:" + bridgePort + "/"));
                    usingHostedBridge = true;
                    MLLog.Msg("[APClient] Routing hosted AP connection through local bridge.");
                }

                string preflightError;
                if (!usingHostedBridge && !CanReachServer(endpoint.Host, endpoint.Port, 5000, out preflightError))
                {
                    MLLog.Error("[APClient] Connect cancelled: " + preflightError);
                    ReleaseConnectionLock();
                    Connecting = false;
                    return;
                }

                ConfigureTlsForEndpoint(endpoint);
                MLLog.Msg("[APClient] Using " + endpoint.Uri.Scheme + " websocket endpoint: " + endpoint.Uri);
                if (!usingHostedBridge)
                    RunConnectionDiagnostics(endpoint);
                Session = ArchipelagoSessionFactory.CreateSession(endpoint.Uri);
                ConfigureSessionWebSocket(Session, endpoint);

                string[] tags = new string[0];
                string uuid = "ArchDandara-" + APConfig.SlotName;
                string password = APConfig.Password == null ? "" : APConfig.Password;

                LoginResult result = Session.TryConnectAndLogin(
                    "Dandara",
                    APConfig.SlotName,
                    ItemsHandlingFlags.AllItems,
                    new Version(0, 6, 5),
                    tags,
                    uuid,
                    password,
                    true);

                if (result is LoginSuccessful)
                {
                    LoginSuccessful success = result as LoginSuccessful;
                    Connected = true;
                    GoalAchievedSent = false;
                    DequeuedItemIndex = 0;
                    MLLog.Msg("[APClient] Connected.");
                    APSaveProfileService.SetActiveProfile(APConfig.SlotName, GetSeedName());
                    HintCache.InitializeForSession(GetSeedName());
                    if (success != null)
                    {
                        // Slot data is generated by the APWorld. It is applied before item replay so
                        // costs, colors, DeathLink, and goal settings are active for all restored state.
                        IDictionary<string, object> slotSettings = APSlotSettingsFile.Resolve(success.SlotData);
                        APSlotSettings.ApplySlotData(slotSettings);
                        ImportBossKeyHints(success.SlotData);
                        ImportItemLocationHints(success.SlotData);
                    }
                    APDeathLink.Initialize(Session, APSlotSettings.DeathLink);
                    SubscribeSocketEvents();
                    SubscribeMessageLog();
                    Connecting = false;
                    if (PendingCurrentSaveResync || SaveSync.NeedsServerResync)
                    {
                        // New-game/reload flow asks the server to replay already-earned items into
                        // the fresh local save instead of trusting stale vanilla save contents.
                        ResyncCurrentSaveFromServer();
                    }
                    else
                    {
                        SyncCheckedLocationsFromServer();
                        SyncReceivedItemCountsFromServer();
                        ShopBarVisualService.RefreshAll();
                    }

                    return;
                }

                LoginFailure failure = result as LoginFailure;
                if (failure != null)
                    MLLog.Error("[APClient] Login failed: " + string.Join(", ", failure.Errors));
                else
                    MLLog.Error("[APClient] Login failed.");

                StopExternalHostedBridge();
                ReleaseConnectionLock();
            }
            catch (Exception ex)
            {
                Connected = false;
                MLLog.Error("[APClient] Connect failed: " + ex);
                StopExternalHostedBridge();
                ReleaseConnectionLock();
            }
            finally
            {
                Connecting = false;
            }
        }

        public static void UpdateConnectionStatus()
        {
            if (!Connected)
                return;

            if (UnityEngine.Time.time < NextConnectionCheckTime)
                return;

            NextConnectionCheckTime = UnityEngine.Time.time + 1.0f;

            try
            {
                if (Session == null || Session.Socket == null || !Session.Socket.Connected)
                    HandleConnectionLost("socket disconnected");
            }
            catch (Exception ex)
            {
                HandleConnectionLost(ex.GetType().Name + ": " + ex.Message);
            }
        }

        public static void Disconnect()
        {
            MLLog.Msg("[APClient] Disconnecting...");

            UnsubscribeSocketEvents();
            Connected = false;
            Connecting = false;
            APDeathLink.Disconnect();
            Session = null;
            ItemLocationCache.Clear();
            LastHintRequestByItem.Clear();
            PendingFreeScoutByItem.Clear();
            StopExternalHostedBridge();
            ReleaseConnectionLock();
        }

        public static void Reconnect()
        {
            if (Connected || Session != null)
                Disconnect();

            Connect();
        }

        public static bool SendLocation(long locationID)
        {
            if (!Connected)
            {
                MLLog.Warning("[APClient] Not connected.");
                return false;
            }

            MLLog.Msg("[APClient] Sending Location: " + locationID);

            try
            {
                Session.Locations.CompleteLocationChecks(locationID);
                return true;
            }
            catch (Exception ex)
            {
                MLLog.Error("[APClient] SendLocation failed: " + ex);
                HandleConnectionLost(ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        public static void SendDeath()
        {
            SendDeath(null);
        }

        public static void SendDeath(string cause)
        {
            if (!Connected)
                return;

            APDeathLink.SendDeath(cause);
        }

        public static bool SendChat(string message)
        {
            if (string.IsNullOrEmpty(message))
                return false;

            if (!Connected || Session == null)
                return false;

            try
            {
                Session.Say(message);
                return true;
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to send chat: " +
                                    ex.GetType().Name + ": " + ex.Message);
                HandleConnectionLost(ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        public static bool SendGoalAchieved(string source)
        {
            if (GoalAchievedSent)
                return false;

            if (!Connected || Session == null)
                return false;

            try
            {
                Session.SetGoalAchieved();
                GoalAchievedSent = true;
                MLLog.Msg("[APClient] Sent goal achieved from " + source + ".");
                return true;
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to send goal achieved from " + source + ": " +
                                    ex.GetType().Name + ": " + ex.Message);
                HandleConnectionLost(ex.GetType().Name + ": " + ex.Message);
                return false;
            }
        }

        public static APItemLocation FindItemLocation(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return APItemLocation.Unknown(itemName);

            APItemLocation cachedHint;
            if (HintCache.TryGet(itemName, out cachedHint))
            {
                ItemLocationCache[itemName] = cachedHint;
                return cachedHint;
            }

            APItemLocation cached;
            if (ItemLocationCache.TryGetValue(itemName, out cached))
                return cached;

            APItemLocation resolved = APItemLocation.Unknown(itemName);
            ItemLocationCache[itemName] = resolved;
            return resolved;
        }

        public static bool RequestHint(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return false;

            if (!Connected || Session == null)
                return false;

            APItemLocation cachedHint;
            if (HintCache.TryGet(itemName, out cachedHint))
            {
                ItemLocationCache[itemName] = cachedHint;
                MLLog.Msg("[APClient] Hint already cached, not requesting again: " + itemName +
                                " | " + cachedHint.PlayerName + " at " + cachedHint.LocationName);
                return false;
            }

            DateTime now = DateTime.UtcNow;
            DateTime lastRequest;
            if (LastHintRequestByItem.TryGetValue(itemName, out lastRequest) &&
                (now - lastRequest).TotalSeconds < HintCooldownSeconds)
                return false;

            LastHintRequestByItem[itemName] = now;
            MLLog.Msg("[APClient] No cached slot-data hint for " + itemName +
                            ". Regenerate with the current Dandara APWorld to enable free in-game hints.");
            return false;
        }

        private static bool RequestFreeLocationScout(string itemName)
        {
            if (itemName != "Boss StoryEvent Key 1" && itemName != "Boss StoryEvent Key 2")
                return false;

            if (Session == null || Session.Socket == null)
                return false;

            PendingFreeScoutByItem[itemName] = true;

            LocationScoutsPacket packet = new LocationScoutsPacket();
            packet.Locations = LocationIds.GetAllLocationIds();
            packet.CreateAsHint = (int)HintCreationPolicy.None;
            Session.Socket.SendPacket(packet);
            return true;
        }

        private static void ImportBossKeyHints(IDictionary<string, object> slotData)
        {
            object hintsObject;
            if (slotData == null || !slotData.TryGetValue("boss_key_hints", out hintsObject) || hintsObject == null)
                return;

            ImportItemLocationHint(hintsObject, "Boss StoryEvent Key 1", true);
            ImportItemLocationHint(hintsObject, "Boss StoryEvent Key 2", true);
        }

        private static void ImportItemLocationHints(IDictionary<string, object> slotData)
        {
            object hintsObject;
            if (slotData == null || !slotData.TryGetValue("item_location_hints", out hintsObject) ||
                hintsObject == null)
                return;

            ImportItemLocationHint(hintsObject, "DLC StoryEvent 1", false);
            ImportItemLocationHint(hintsObject, "DLC StoryEvent 2", false);
            ImportItemLocationHint(hintsObject, "DLC StoryEvent 3", false);
            ImportItemLocationHint(hintsObject, "DLC StoryEvent 4", false);
            ImportItemLocationHint(hintsObject, "DLC StoryEvent 5", false);
            ImportItemLocationHint(hintsObject, "DLC StoryEvent 6", false);
            ImportItemLocationHint(hintsObject, "DLC StoryEvent 7", false);
            ImportItemLocationHint(hintsObject, "TimeFlag", false);
        }

        private static void ImportItemLocationHint(object hintsObject, string itemName, bool notifyGate)
        {
            object hintObject;
            if (!TryGetNestedValue(hintsObject, itemName, out hintObject) || hintObject == null)
                return;

            object playerObject;
            object locationObject;
            if (!TryGetNestedValue(hintObject, "player", out playerObject) ||
                !TryGetNestedValue(hintObject, "location", out locationObject))
                return;

            string playerName = ToDataString(playerObject);
            string locationName = ToDataString(locationObject);
            if (string.IsNullOrEmpty(locationName))
                return;

            APItemLocation itemLocation = new APItemLocation(itemName,
                string.IsNullOrEmpty(playerName) ? "unknown player" : playerName,
                locationName);
            ItemLocationCache[itemName] = itemLocation;
            HintCache.Store(itemLocation);
            if (notifyGate)
                InteractionGateService.OnHintLocationUpdated(itemLocation);
            MLLog.Msg("[APClient] Imported slot-data item hint: " + itemName +
                            " | " + itemLocation.PlayerName + " at " + itemLocation.LocationName);
        }

        private static bool TryGetNestedValue(object source, string key, out object value)
        {
            value = null;
            if (source == null || string.IsNullOrEmpty(key))
                return false;

            IDictionary<string, object> dictionary = source as IDictionary<string, object>;
            if (dictionary != null)
                return dictionary.TryGetValue(key, out value);

            JObject jObject = source as JObject;
            if (jObject != null)
            {
                JToken token;
                if (jObject.TryGetValue(key, out token))
                {
                    value = token;
                    return true;
                }

                return false;
            }

            JToken jToken = source as JToken;
            if (jToken != null && jToken.Type == JTokenType.Object)
            {
                JToken token = jToken[key];
                if (token != null)
                {
                    value = token;
                    return true;
                }
            }

            return false;
        }

        private static string ToDataString(object value)
        {
            if (value == null)
                return "";

            JValue jValue = value as JValue;
            if (jValue != null)
                return jValue.Value == null ? "" : jValue.Value.ToString();

            JToken jToken = value as JToken;
            if (jToken != null)
                return jToken.Type == JTokenType.Null ? "" : jToken.ToString();

            return value.ToString();
        }

        private static bool CanReachServer(string host, int port, int timeoutMilliseconds, out string error)
        {
            error = null;
            TcpClient client = null;

            try
            {
                client = new TcpClient();
                IAsyncResult result = client.BeginConnect(host, port, null, null);
                bool connected = result.AsyncWaitHandle.WaitOne(timeoutMilliseconds, false);
                if (!connected)
                {
                    error = "timed out reaching " + host + ":" + port +
                            ". Check that the AP server is running on that port.";
                    return false;
                }

                client.EndConnect(result);
                return true;
            }
            catch (Exception ex)
            {
                error = "could not reach " + host + ":" + port + " (" +
                        ex.GetType().Name + ": " + ex.Message + ")";
                return false;
            }
            finally
            {
                if (client != null)
                    client.Close();
            }

        }

        private static ServerEndpoint ResolveServerEndpoint(string serverAddress, int serverPort)
        {
            string address = string.IsNullOrEmpty(serverAddress) ? "localhost" : serverAddress.Trim();
            Uri uri;

            if (Uri.TryCreate(address, UriKind.Absolute, out uri) &&
                (uri.Scheme == "ws" || uri.Scheme == "wss"))
                return new ServerEndpoint(uri);

            string scheme = IsArchipelagoHostedAddress(address) ? "wss" : "ws";
            UriBuilder builder = new UriBuilder(scheme, address, serverPort);
            return new ServerEndpoint(builder.Uri);
        }

        private static bool IsArchipelagoHostedAddress(string address)
        {
            return string.Equals(address, "archipelago.gg", StringComparison.OrdinalIgnoreCase) ||
                   address.EndsWith(".archipelago.gg", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ShouldUseHostedBridge(ServerEndpoint endpoint)
        {
            return !object.ReferenceEquals(endpoint, null) &&
                   endpoint.Uri.Scheme == "wss" &&
                   IsArchipelagoHostedAddress(endpoint.Host);
        }

        private static bool EnsureHostedBridge(ServerEndpoint targetEndpoint, int bridgePort)
        {
            string externalError;
            if (EnsureExternalHostedBridge(targetEndpoint, bridgePort, out externalError))
                return true;

            MLLog.Warning("[APClient] External AP WSS bridge unavailable: " + externalError);
            MLLog.Warning("[APClient] Hosted AP rooms require the external bridge because Dandara's Mono TLS cannot connect to wss:// directly.");

            string bridgeError;
            if (!InProcessWssBridge.EnsureStarted(targetEndpoint.Uri, bridgePort, out bridgeError))
            {
                MLLog.Error("[APClient] Failed to start in-process AP WSS bridge: " + bridgeError);
                return false;
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(5.0);
            while (DateTime.UtcNow < deadline)
            {
                string error;
                if (CanReachServer("127.0.0.1", bridgePort, 250, out error))
                    return true;

                System.Threading.Thread.Sleep(100);
            }

            MLLog.Error("[APClient] In-process AP WSS bridge did not become reachable on port " + bridgePort + ".");
            return false;
        }

        private static bool EnsureExternalHostedBridge(ServerEndpoint targetEndpoint, int bridgePort, out string error)
        {
            error = null;

            string bridgePath = System.IO.Path.Combine(APConfig.DataFolder, ExternalBridgeRelativePath);
            string bridgeLogPath = GetExternalBridgeLogPath(targetEndpoint, bridgePort);
            if (!System.IO.File.Exists(bridgePath))
            {
                error = "missing " + bridgePath;
                return false;
            }

            WaitForBridgePortToClose(bridgePort);
            TryClearExternalBridgeLog(bridgeLogPath);

            try
            {
                string arguments = "\"" + targetEndpoint.Uri + "\" " + bridgePort + " \"" + bridgeLogPath + "\"";
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = bridgePath;
                startInfo.Arguments = arguments;
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(bridgePath);
                ExternalBridgeProcess = Process.Start(startInfo);
                MLLog.Msg("[APClient] Started external AP WSS bridge: " + bridgePath +
                          " pid=" + (ExternalBridgeProcess == null ? "<unknown>" : ExternalBridgeProcess.Id.ToString()));
                MLLog.Msg("[APClient] External AP WSS bridge log: " + bridgeLogPath);
            }
            catch (Exception ex)
            {
                error = ex.GetType().Name + ": " + ex.Message;
                return false;
            }

            DateTime deadline = DateTime.UtcNow.AddSeconds(5.0);
            while (DateTime.UtcNow < deadline)
            {
                string reachError;
                if (CanReachServer("127.0.0.1", bridgePort, 250, out reachError))
                    return true;

                System.Threading.Thread.Sleep(100);
            }

            error = "external bridge did not become reachable on port " + bridgePort;
            return false;
        }

        private static void TryClearExternalBridgeLog(string bridgeLogPath)
        {
            try
            {
                string directory = System.IO.Path.GetDirectoryName(bridgeLogPath);
                if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);

                System.IO.File.WriteAllText(bridgeLogPath, "");
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to clear external bridge log: " +
                              ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void KillExistingExternalBridgeProcesses()
        {
            try
            {
                Process[] processes = Process.GetProcessesByName("ArchipelagoWssBridge");
                for (int i = 0; i < processes.Length; i++)
                {
                    Process process = processes[i];
                    try
                    {
                        if (process == null || process.HasExited)
                            continue;

                        MLLog.Msg("[APClient] Closing stale external AP WSS bridge process " + process.Id + ".");
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        MLLog.Warning("[APClient] Failed to close stale AP WSS bridge: " +
                                      ex.GetType().Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to enumerate AP WSS bridge processes: " +
                              ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void StopExternalHostedBridge()
        {
            try
            {
                if (ExternalBridgeProcess != null && !ExternalBridgeProcess.HasExited)
                {
                    MLLog.Msg("[APClient] Closing external AP WSS bridge process " + ExternalBridgeProcess.Id + ".");
                    ExternalBridgeProcess.Kill();
                }
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to close external AP WSS bridge: " +
                              ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                ExternalBridgeProcess = null;
            }
        }

        private static void WaitForBridgePortToClose(int bridgePort)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(2.0);
            while (DateTime.UtcNow < deadline)
            {
                string reachError;
                if (!CanReachServer("127.0.0.1", bridgePort, 100, out reachError))
                    return;

                System.Threading.Thread.Sleep(100);
            }
        }

        private static int GetHostedBridgePort(ServerEndpoint targetEndpoint)
        {
            int preferredPort = HostedBridgeBasePort + (PositiveHash(GetConnectionKey(targetEndpoint)) % 20000);
            for (int i = 0; i < 1000; i++)
            {
                int port = preferredPort + i;
                if (port > 59999)
                    port = HostedBridgeBasePort + (port - 60000);

                string reachError;
                if (!CanReachServer("127.0.0.1", port, 100, out reachError))
                    return port;
            }

            return preferredPort;
        }

        private static bool TryAcquireConnectionLock(ServerEndpoint endpoint, out string error)
        {
            error = null;
            if (ActiveConnectionLock != null)
                return true;

            try
            {
                string directory = System.IO.Path.Combine(APConfig.DataFolder, "ActiveConnections");
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);

                string key = GetConnectionKey(endpoint);
                string fileName = SanitizeFileName(key) + ".lock";
                if (fileName.Length > 120)
                    fileName = PositiveHash(key).ToString("X8") + ".lock";

                string path = System.IO.Path.Combine(directory, fileName);
                System.IO.FileStream stream = new System.IO.FileStream(path,
                    System.IO.FileMode.OpenOrCreate,
                    System.IO.FileAccess.ReadWrite,
                    System.IO.FileShare.None);

                string details = "pid=" + Process.GetCurrentProcess().Id +
                                 "\r\nserver=" + endpoint.Host + ":" + endpoint.Port +
                                 "\r\nslot=" + APConfig.SlotName +
                                 "\r\nstarted=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n";
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(details);
                stream.SetLength(0);
                stream.Write(bytes, 0, bytes.Length);
                stream.Flush();

                ActiveConnectionLock = stream;
                ActiveConnectionLockPath = path;
                return true;
            }
            catch (System.IO.IOException)
            {
                error = "another Dandara instance is already connected or connecting with this same server and slot. " +
                        "Use a different SlotName before connecting a second game.";
                return false;
            }
            catch (Exception ex)
            {
                error = "could not create active connection lock (" +
                        ex.GetType().Name + ": " + ex.Message + ")";
                return false;
            }
        }

        private static void ReleaseConnectionLock()
        {
            try
            {
                if (ActiveConnectionLock != null)
                    ActiveConnectionLock.Close();
            }
            catch
            {
            }
            finally
            {
                ActiveConnectionLock = null;
            }

            try
            {
                if (!string.IsNullOrEmpty(ActiveConnectionLockPath) &&
                    System.IO.File.Exists(ActiveConnectionLockPath))
                    System.IO.File.Delete(ActiveConnectionLockPath);
            }
            catch
            {
            }
            finally
            {
                ActiveConnectionLockPath = null;
            }
        }

        private static string GetExternalBridgeLogPath(ServerEndpoint targetEndpoint, int bridgePort)
        {
            string directory = System.IO.Path.Combine(APConfig.DataFolder, ExternalBridgeLogFolderRelativePath);
            string fileName = "ArchipelagoWssBridge_" +
                              SanitizeFileName(targetEndpoint.Host + "_" + targetEndpoint.Port + "_" +
                                               APConfig.SlotName + "_" + bridgePort) + ".log";
            return System.IO.Path.Combine(directory, fileName);
        }

        private static string GetConnectionKey(ServerEndpoint endpoint)
        {
            string slot = string.IsNullOrEmpty(APConfig.SlotName) ? "unknown" : APConfig.SlotName.Trim();
            return endpoint.Host.ToLowerInvariant() + "_" + endpoint.Port + "_" + slot.ToLowerInvariant();
        }

        private static int PositiveHash(string value)
        {
            unchecked
            {
                int hash = 23;
                if (value != null)
                {
                    for (int i = 0; i < value.Length; i++)
                        hash = hash * 31 + value[i];
                }

                return hash & 0x7fffffff;
            }
        }

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "empty";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == ' ' || chars[i] == ':' || chars[i] == '/' || chars[i] == '\\')
                {
                    chars[i] = '_';
                    continue;
                }

                for (int j = 0; j < invalid.Length; j++)
                {
                    if (chars[i] == invalid[j])
                    {
                        chars[i] = '_';
                        break;
                    }
                }
            }

            return new string(chars);
        }

        private static void ConfigureTlsForEndpoint(ServerEndpoint endpoint)
        {
            if (object.ReferenceEquals(endpoint, null) || endpoint.Uri.Scheme != "wss")
                return;

            try
            {
                ServicePointManager.SecurityProtocol =
                    (SecurityProtocolType)3072;
                MLLog.Msg("[APClient] Enabled TLS 1.2 for secure AP websocket.");
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to configure TLS protocols: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void RunConnectionDiagnostics(ServerEndpoint endpoint)
        {
            try
            {
                LogLoadedAssembly("Archipelago.MultiClient.Net", typeof(ArchipelagoSessionFactory).Assembly);
                LogLoadedAssembly("websocket-sharp", typeof(WebSocket).Assembly);
                LogLoadedAssembly("Newtonsoft.Json", typeof(Newtonsoft.Json.JsonConvert).Assembly);

                if (endpoint.Uri.Scheme != "wss")
                    return;

                RunTlsDiagnostics(endpoint);

                DiagnosticProbeOpened = false;
                DiagnosticProbeError = "";
                DiagnosticProbeClose = "";

                using (WebSocket socket = new WebSocket(endpoint.Uri.ToString()))
                {
                    ConfigureWebSocketForHostedTls(socket, endpoint, "[APClient][Diag]");
                    socket.Log.Level = LogLevel.Trace;
                    socket.WaitTime = TimeSpan.FromSeconds(6.0);

                    socket.OnOpen += OnDiagnosticWebSocketOpen;
                    socket.OnError += OnDiagnosticWebSocketError;
                    socket.OnClose += OnDiagnosticWebSocketClose;

                    MLLog.Msg("[APClient][Diag] Starting direct websocket probe to " + endpoint.Uri);
                    socket.Connect();
                    MLLog.Msg("[APClient][Diag] Direct websocket probe result: opened=" + DiagnosticProbeOpened +
                                    " alive=" + socket.IsAlive +
                                    " state=" + socket.ReadyState +
                                    " error=" + (string.IsNullOrEmpty(DiagnosticProbeError) ? "none" : DiagnosticProbeError) +
                                    " close=" + (string.IsNullOrEmpty(DiagnosticProbeClose) ? "none" : DiagnosticProbeClose));
                    if (DiagnosticProbeOpened || socket.IsAlive)
                        socket.Close();
                }
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient][Diag] Diagnostics failed: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void RunTlsDiagnostics(ServerEndpoint endpoint)
        {
            TcpClient client = null;
            SslStream sslStream = null;

            try
            {
                TlsProbeHost = endpoint.Host;
                client = new TcpClient();
                IAsyncResult result = client.BeginConnect(endpoint.Host, endpoint.Port, null, null);
                if (!result.AsyncWaitHandle.WaitOne(5000, false))
                {
                    MLLog.Warning("[APClient][Diag] TLS probe TCP connect timed out.");
                    return;
                }

                client.EndConnect(result);
                sslStream = new SslStream(client.GetStream(), false, ValidateTlsProbeCertificate);
                MLLog.Msg("[APClient][Diag] Starting raw TLS probe to " + endpoint.Host + ":" + endpoint.Port);
                sslStream.AuthenticateAsClient(endpoint.Host, null, SslProtocols.Tls12, false);
                MLLog.Msg("[APClient][Diag] Raw TLS probe succeeded: protocol=" + sslStream.SslProtocol +
                                " cipher=" + sslStream.CipherAlgorithm +
                                " strength=" + sslStream.CipherStrength);
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient][Diag] Raw TLS probe failed: " +
                                    ex.GetType().Name + ": " + ex.Message);
                if (ex.InnerException != null)
                    MLLog.Warning("[APClient][Diag] Raw TLS inner exception: " +
                                        ex.InnerException.GetType().Name + ": " +
                                        ex.InnerException.Message);
            }
            finally
            {
                if (sslStream != null)
                    sslStream.Close();
                if (client != null)
                    client.Close();
            }
        }

        private static void ConfigureSessionWebSocket(ArchipelagoSession session, ServerEndpoint endpoint)
        {
            if (endpoint.Uri.Scheme != "wss" || object.ReferenceEquals(session, null) ||
                object.ReferenceEquals(session.Socket, null))
                return;

            try
            {
                FieldInfo field = session.Socket.GetType().GetField("webSocket",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                if (object.ReferenceEquals(field, null))
                {
                    MLLog.Warning("[APClient][Diag] Could not find ArchipelagoNet private webSocket field.");
                    return;
                }

                WebSocket socket = field.GetValue(session.Socket) as WebSocket;
                if (object.ReferenceEquals(socket, null))
                {
                    MLLog.Warning("[APClient][Diag] ArchipelagoNet private webSocket field was null.");
                    return;
                }

                ConfigureWebSocketForHostedTls(socket, endpoint, "[APClient]");
                socket.Log.Level = LogLevel.Warn;
                MLLog.Msg("[APClient] Configured ArchipelagoNet websocket TLS settings.");
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient][Diag] Failed to configure ArchipelagoNet websocket: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void ConfigureWebSocketForHostedTls(WebSocket socket, ServerEndpoint endpoint, string logPrefix)
        {
            if (object.ReferenceEquals(socket, null) || endpoint.Uri.Scheme != "wss")
                return;

            socket.SslConfiguration.EnabledSslProtocols =
                SslProtocols.Tls12;
            socket.SslConfiguration.CheckCertificateRevocation = false;
            socket.SslConfiguration.TargetHost = endpoint.Host;
            TlsValidationLogPrefix = logPrefix;
            socket.SslConfiguration.ServerCertificateValidationCallback = ValidateHostedApCertificate;
        }

        private static void OnDiagnosticWebSocketOpen(object sender, EventArgs args)
        {
            DiagnosticProbeOpened = true;
            MLLog.Msg("[APClient][Diag] Direct websocket probe opened.");
        }

        private static void OnDiagnosticWebSocketError(object sender, ErrorEventArgs args)
        {
            DiagnosticProbeError = args == null ? "unknown websocket error" : args.Message;
            MLLog.Warning("[APClient][Diag] Direct websocket probe error: " + DiagnosticProbeError);
            if (args != null && args.Exception != null)
                MLLog.Warning("[APClient][Diag] Direct websocket probe exception: " +
                                    args.Exception.GetType().Name + ": " +
                                    args.Exception.Message);
        }

        private static void OnDiagnosticWebSocketClose(object sender, CloseEventArgs args)
        {
            if (args != null)
                DiagnosticProbeClose = args.Code + " " + args.Reason + " clean=" + args.WasClean;
            MLLog.Msg("[APClient][Diag] Direct websocket probe closed: " + DiagnosticProbeClose);
        }

        private static bool ValidateHostedApCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            MLLog.Msg(TlsValidationLogPrefix + " TLS cert subject=" +
                            (certificate == null ? "null" : certificate.Subject) +
                            " | errors=" + sslPolicyErrors);
            if (chain != null && chain.ChainStatus != null)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                    MLLog.Msg(TlsValidationLogPrefix + " TLS chain " + i + ": " +
                                    chain.ChainStatus[i].Status + " " +
                                    chain.ChainStatus[i].StatusInformation);
            }

            if (sslPolicyErrors != SslPolicyErrors.None)
                MLLog.Warning(TlsValidationLogPrefix +
                                    " Accepting hosted AP TLS certificate despite validation errors.");

            return true;
        }

        private static bool ValidateTlsProbeCertificate(object sender, X509Certificate certificate,
            X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            MLLog.Msg("[APClient][Diag] Raw TLS cert host=" + TlsProbeHost +
                            " subject=" + (certificate == null ? "null" : certificate.Subject) +
                            " | errors=" + sslPolicyErrors);
            if (chain != null && chain.ChainStatus != null)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                    MLLog.Msg("[APClient][Diag] Raw TLS chain " + i + ": " +
                                    chain.ChainStatus[i].Status + " " +
                                    chain.ChainStatus[i].StatusInformation);
            }

            return true;
        }

        private static void LogLoadedAssembly(string label, Assembly assembly)
        {
            try
            {
                AssemblyName name = assembly.GetName();
                MLLog.Msg("[APClient][Diag] Assembly " + label +
                                " version=" + name.Version +
                                " location=" + assembly.Location);
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient][Diag] Failed to log assembly " + label + ": " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private class ServerEndpoint
        {
            public readonly Uri Uri;
            public readonly string Host;
            public readonly int Port;

            public ServerEndpoint(Uri uri)
            {
                Uri = uri;
                Host = uri.Host;
                Port = uri.Port;
            }
        }

        private static void SubscribeSocketEvents()
        {
            try
            {
                if (Session == null || Session.Socket == null)
                    return;

                Session.Socket.SocketClosed -= OnSocketClosed;
                Session.Socket.SocketClosed += OnSocketClosed;
                Session.Socket.PacketReceived -= OnPacketReceived;
                Session.Socket.PacketReceived += OnPacketReceived;
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to subscribe AP socket events: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void UnsubscribeSocketEvents()
        {
            try
            {
                if (Session == null || Session.Socket == null)
                    return;

                Session.Socket.SocketClosed -= OnSocketClosed;
                Session.Socket.PacketReceived -= OnPacketReceived;
            }
            catch
            {
            }
        }

        private static void OnSocketClosed(string reason)
        {
            HandleConnectionLost(string.IsNullOrEmpty(reason) ? "socket closed" : reason);
        }

        private static void OnPacketReceived(ArchipelagoPacketBase packet)
        {
            if (PendingFreeScoutByItem.Count > 0 && packet != null)
                MLLog.Msg("[APClient] Free boss gate scout received packet: " + packet.PacketType);

            LocationInfoPacket locationInfo = packet as LocationInfoPacket;
            if (locationInfo == null || locationInfo.Locations == null || PendingFreeScoutByItem.Count == 0)
                return;

            MLLog.Msg("[APClient] Free boss gate scout response locations=" + locationInfo.Locations.Length +
                            " pending=" + PendingFreeScoutByItem.Count);

            for (int i = 0; i < locationInfo.Locations.Length; i++)
                TryResolveFreeScout(locationInfo.Locations[i]);
        }

        private static void TryResolveFreeScout(NetworkItem networkItem)
        {
            string itemName = GetDandaraItemName(networkItem.Item);
            if (string.IsNullOrEmpty(itemName))
                itemName = GetKnownDandaraItemName(networkItem.Item);

            string pendingItemName = FindPendingFreeScoutItem(itemName);
            if (string.IsNullOrEmpty(pendingItemName))
            {
                LogFreeScoutCandidate(networkItem, itemName);
                return;
            }

            int slot = GetActiveSlot();
            if (slot > 0 && networkItem.Player != slot)
                return;

            string locationName = GetDandaraLocationName(networkItem.Location);
            string playerName = GetPlayerName(networkItem.Player);
            APItemLocation itemLocation = new APItemLocation(pendingItemName, playerName, locationName);
            ItemLocationCache[pendingItemName] = itemLocation;
            HintCache.Store(itemLocation);
            PendingFreeScoutByItem.Remove(pendingItemName);
            InteractionGateService.OnHintLocationUpdated(itemLocation);
            MLLog.Msg("[APClient] Free boss gate scout resolved: " + pendingItemName +
                            " | " + playerName + " at " + locationName);
        }

        private static string FindPendingFreeScoutItem(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return null;

            if (PendingFreeScoutByItem.ContainsKey(itemName))
                return itemName;

            string normalizedItemName = NormalizeHintName(itemName);
            foreach (string pendingItemName in PendingFreeScoutByItem.Keys)
            {
                if (NormalizeHintName(pendingItemName) == normalizedItemName)
                    return pendingItemName;
            }

            return null;
        }

        private static string NormalizeHintName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string normalized = "";
            for (int i = 0; i < value.Length; i++)
            {
                char c = char.ToLowerInvariant(value[i]);
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                    normalized += c;
            }

            return normalized;
        }

        private static void LogFreeScoutCandidate(NetworkItem networkItem, string itemName)
        {
            string locationName = GetDandaraLocationName(networkItem.Location);
            if (!IsKnownBossGateCandidateLocation(locationName) &&
                networkItem.Item != 4 && networkItem.Item != 5)
                return;

            MLLog.Msg("[APClient] Free boss gate scout candidate: itemId=" + networkItem.Item +
                            " itemName=" + (string.IsNullOrEmpty(itemName) ? "<unresolved>" : itemName) +
                            " locationId=" + networkItem.Location +
                            " locationName=" + locationName +
                            " player=" + networkItem.Player + "/" + GetPlayerName(networkItem.Player));
        }

        private static bool IsKnownBossGateCandidateLocation(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
                return false;

            return locationName.IndexOf("The Grand Stage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   locationName.IndexOf("Overcast Gate Ruins", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   locationName.IndexOf("Temple of Creation", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string GetDandaraItemName(long itemId)
        {
            try
            {
                if (Session != null && Session.Items != null)
                    return Session.Items.GetItemName(itemId, "Dandara");
            }
            catch
            {
            }

            return "";
        }

        private static string GetKnownDandaraItemName(long itemId)
        {
            if (itemId == 4)
                return "Boss StoryEvent Key 1";

            if (itemId == 5)
                return "Boss StoryEvent Key 2";

            return "";
        }

        private static string GetDandaraLocationName(long locationId)
        {
            try
            {
                if (Session != null && Session.Locations != null)
                    return Session.Locations.GetLocationNameFromId(locationId, "Dandara");
            }
            catch
            {
            }

            string localLocationName;
            if (LocationIds.TryGetLocationNameById(locationId, out localLocationName))
                return localLocationName;

            return "unknown location";
        }

        private static int GetActiveSlot()
        {
            try
            {
                return Session != null && Session.ConnectionInfo != null ? Session.ConnectionInfo.Slot : 0;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetPlayerName(int slot)
        {
            try
            {
                if (!object.ReferenceEquals(Session, null) && !object.ReferenceEquals(Session.Players, null))
                {
                    string name = Session.Players.GetPlayerAlias(slot);
                    if (!string.IsNullOrEmpty(name))
                        return name;

                    name = Session.Players.GetPlayerName(slot);
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch
            {
            }

            return slot == GetActiveSlot() && !string.IsNullOrEmpty(APConfig.SlotName)
                ? APConfig.SlotName
                : "unknown player";
        }

        private static string GetActivePlayerName()
        {
            try
            {
                if (!object.ReferenceEquals(Session, null) &&
                    !object.ReferenceEquals(Session.Players, null) &&
                    !object.ReferenceEquals(Session.Players.ActivePlayer, null))
                    return GetPlayerName(Session.Players.ActivePlayer);
            }
            catch
            {
            }

            return string.IsNullOrEmpty(APConfig.SlotName) ? "unknown player" : APConfig.SlotName;
        }

        private static void HandleConnectionLost(string reason)
        {
            if (!Connected)
                return;

            MLLog.Warning("[APClient] Disconnected from server: " + reason);
            UnsubscribeSocketEvents();
            Connected = false;
            APDeathLink.Disconnect();
            Session = null;
            ItemLocationCache.Clear();
            LastHintRequestByItem.Clear();
            PendingFreeScoutByItem.Clear();
            StopExternalHostedBridge();
            ReleaseConnectionLock();
            ShowDisconnectedMessage();
        }

        private static void ShowDisconnectedMessage()
        {
            try
            {
                HUDManager hudManager = GameAccess.HudManager;
                if (hudManager != null)
                    hudManager.ShowSmallText("Archipelago server disconnected. Press F4 to reconnect.");
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to show disconnect message: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void SubscribeMessageLog()
        {
            try
            {
                if (Session == null || Session.MessageLog == null)
                    return;

                Session.MessageLog.OnMessageReceived -= OnMessageReceived;
                Session.MessageLog.OnMessageReceived += OnMessageReceived;
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to subscribe AP message log: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void OnMessageReceived(LogMessage message)
        {
            APDeathLink.OnMessage(message);

            HintItemSendLogMessage hint = message as HintItemSendLogMessage;
            if (hint == null || hint.Item == null)
                return;

            string itemName = hint.Item.ItemName;
            if (string.IsNullOrEmpty(itemName))
                itemName = hint.Item.ItemDisplayName;
            if (string.IsNullOrEmpty(itemName))
                return;

            string playerName = GetPlayerName(hint.Sender);
            string locationName = hint.Item.LocationName;
            if (string.IsNullOrEmpty(locationName))
                locationName = hint.Item.LocationDisplayName;
            if (string.IsNullOrEmpty(locationName))
                locationName = "unknown location";

            APItemLocation itemLocation = new APItemLocation(itemName, playerName, locationName);
            ItemLocationCache[itemName] = itemLocation;
            HintCache.Store(itemLocation);
            InteractionGateService.OnHintLocationUpdated(itemLocation);
            MLLog.Msg("[APClient] Hint resolved: " + itemName + " | " + playerName + " at " + locationName);
        }

        private static string GetSeedName()
        {
            try
            {
                if (Session != null && Session.RoomState != null && !string.IsNullOrEmpty(Session.RoomState.Seed))
                    return Session.RoomState.Seed;
            }
            catch (Exception ex)
            {
                MLLog.Warning("[APClient] Failed to read AP seed name: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }

            return APConfig.ServerAddress + "_" + APConfig.ServerPort;
        }

        private static string GetPlayerName(global::Archipelago.MultiClient.Net.Helpers.PlayerInfo player)
        {
            if (object.ReferenceEquals(player, null))
                return "unknown player";

            if (!string.IsNullOrEmpty(player.Alias))
                return player.Alias;

            if (!string.IsNullOrEmpty(player.Name))
                return player.Name;

            return "unknown player";
        }

        public static void PollReceivedItems()
        {
            if (!Connected || Session == null || Session.Items == null)
                return;

            if (!GameAccess.IsReadyForApItemGrants)
                return;

            try
            {
                int skippedAlreadyProcessed = 0;
                while (Session.Items.Any())
                {
                    ItemInfo item = Session.Items.DequeueItem();
                    DequeuedItemIndex++;

                    if (DequeuedItemIndex <= SaveSync.ProcessedReceivedItemCount)
                    {
                        skippedAlreadyProcessed++;
                        continue;
                    }

                    string itemName = item.ItemName;
                    if (string.IsNullOrEmpty(itemName))
                        itemName = item.ItemDisplayName;

                    MLLog.Msg("[APClient] Received item " + DequeuedItemIndex + ": " + itemName + " (" +
                                    item.ItemId + ")");
                    APItemReceiver.Enqueue(DequeuedItemIndex, itemName);
                }

                if (skippedAlreadyProcessed > 0)
                    MLLog.Msg("[APClient] Skipped already processed AP items: " + skippedAlreadyProcessed);
            }
            catch (Exception ex)
            {
                MLLog.Error("[APClient] PollReceivedItems failed: " + ex);
            }
        }

        public static void ResyncCurrentSaveFromServer()
        {
            if (!Connected || Session == null || Session.Items == null)
            {
                PendingCurrentSaveResync = true;
                SaveSync.SetServerResyncNeeded(true);
                MLLog.Msg("[APClient] AP resync queued until F3 connects to the server.");
                return;
            }

            PendingCurrentSaveResync = false;
            SaveSync.SetServerResyncNeeded(false);
            SyncCheckedLocationsFromServer();
            ShopSaltBalanceService.RecalculateSpentFromCheckedShopLocations();
            ReplayAllReceivedItems();
            ShopBarVisualService.RefreshAll();
        }

        public static void RequestCurrentSaveResync()
        {
            PendingCurrentSaveResync = true;
            SaveSync.SetServerResyncNeeded(true);
            ResyncCurrentSaveFromServer();
        }

        private static void SyncReceivedItemCountsFromServer()
        {
            try
            {
                if (Session == null || Session.Items == null)
                    return;

                ReadOnlyCollection<ItemInfo> allItems = Session.Items.AllItemsReceived;
                if (allItems == null)
                    return;

                string[] itemNames = new string[allItems.Count];
                for (int i = 0; i < allItems.Count; i++)
                {
                    string itemName = allItems[i].ItemName;
                    if (string.IsNullOrEmpty(itemName))
                        itemName = allItems[i].ItemDisplayName;

                    itemNames[i] = itemName;
                }

                SaveSync.ReplaceReceivedItemCounts(itemNames);
            }
            catch (Exception ex)
            {
                MLLog.Error("[APClient] SyncReceivedItemCountsFromServer failed: " + ex);
            }
        }

        private static void SyncCheckedLocationsFromServer()
        {
            try
            {
                if (Session == null || Session.Locations == null ||
                    Session.Locations.AllLocationsChecked == null)
                    return;

                SaveSync.ImportCheckedLocations(Session.Locations.AllLocationsChecked);
                ShopBarVisualService.RefreshAll();
            }
            catch (Exception ex)
            {
                MLLog.Error("[APClient] SyncCheckedLocationsFromServer failed: " + ex);
            }
        }

        private static void ReplayAllReceivedItems()
        {
            try
            {
                ReadOnlyCollection<ItemInfo> allItems = Session.Items.AllItemsReceived;
                if (allItems == null)
                    return;

                APItemReceiver.Clear();
                DequeuedItemIndex = allItems.Count;

                string[] itemNames = new string[allItems.Count];

                for (int i = 0; i < allItems.Count; i++)
                {
                    ItemInfo item = allItems[i];
                    string itemName = item.ItemName;
                    if (string.IsNullOrEmpty(itemName))
                        itemName = item.ItemDisplayName;

                    itemNames[i] = itemName;
                    APItemReceiver.EnqueueReplay(i + 1, itemName);
                }

                SaveSync.ReplaceReceivedItemCounts(itemNames);
                MLLog.Msg("[APClient] Requeued AP items for current save: " + allItems.Count);
            }
            catch (Exception ex)
            {
                MLLog.Error("[APClient] ReplayAllReceivedItems failed: " + ex);
            }
        }
    }

    public class APItemLocation
    {
        public readonly string ItemName;
        public readonly string PlayerName;
        public readonly string LocationName;

        public APItemLocation(string itemName, string playerName, string locationName)
        {
            ItemName = itemName;
            PlayerName = string.IsNullOrEmpty(playerName) ? "unknown player" : playerName;
            LocationName = string.IsNullOrEmpty(locationName) ? "unknown location" : locationName;
        }

        public static APItemLocation Unknown(string itemName)
        {
            return new APItemLocation(itemName, "unknown player", "unknown location");
        }
    }
}

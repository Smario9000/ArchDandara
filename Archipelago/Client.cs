// Client.cs
// Handles connecting this mod to an Archipelago server.
// It also reloads connection settings, manages reconnect logic,
// and listens for socket events like disconnects or errors.

using System;
using MelonLoader;
using UnityEngine;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using ArchDandara.Database;

namespace ArchDandara.Archipelago
{
    public static class APClient
    {
        // Holds the active Archipelago session object after connecting.
        public static ArchipelagoSession Session;

        // MelonPreferences category used to store connection config in the cfg file.
        private static MelonPreferences_Category _category;

        // Stored config values for AP connection.
        private static MelonPreferences_Entry<string> _host;
        private static MelonPreferences_Entry<int> _port;
        private static MelonPreferences_Entry<string> _playerName;
        private static MelonPreferences_Entry<string> _password;

        // Tracks reconnect attempts if connection is lost.
        private static int _reconnectAttempts;
        private const int MaxReconnectAttempts = 5;
        private static bool _isReconnecting;

        // Returns true only if the AP session and its socket both exist and are connected.
        public static bool IsConnected
        {
            get
            {
                return Session != null && Session.Socket != null && Session.Socket.Connected;
            }
        }

        // Creates the Archipelago config entries if they do not already exist.
        public static void InitConfig()
        {
            _category = MelonPreferences.CreateCategory("Archipelago");

            _host = _category.CreateEntry("Host", "localhost");
            _port = _category.CreateEntry("Port", 38281);
            _playerName = _category.CreateEntry("PlayerName", "Player1");
            _password = _category.CreateEntry("Password", "");

            _category.SaveToFile();
            MelonLogger.Msg("Archipelago config initialized");
        }

        // Reloads the AP config from file while the game is running.
        // If already connected, it disconnects and reconnects with the new values.
        public static void ReloadConfig()
        {
            MelonLogger.Msg("Reloading Archipelago config...");
            _category.LoadFromFile();

            MelonLogger.Msg("Config Values:");
            MelonLogger.Msg("Host: " + _host.Value);
            MelonLogger.Msg("Port: " + _port.Value);
            MelonLogger.Msg("Player: " + _playerName.Value);
            MelonLogger.Msg("Password: " + (string.IsNullOrEmpty(_password.Value) ? "<empty>" : "<set>"));

            if (IsConnected)
            {
                MelonLogger.Warning("Already connected. Reconnecting with new config...");
                Disconnect("Reload config");
                Connect();
            }
            else
            {
                MelonLogger.Msg("Not connected. Press F5 to connect.");
            }
        }

        // Tries to connect to the Archipelago server using the saved config values.
        public static void Connect(bool isReconnect = false)
        {
            if (IsConnected)
            {
                MelonLogger.Warning("Already connected.");
                return;
            }

            string host = _host.Value;
            int port = _port.Value;
            string player = _playerName.Value;
            string password = _password.Value;

            MelonLogger.Msg("Connecting to " + host + ":" + port + " as " + player + "...");

            // Writes a log entry into your mod's activity log.
            DataManager.LogActivity(
                "APConnectAttempt",
                GetCurrentScene(),
                player,
                host + ":" + port);

            try
            {
                // Creates a new AP session object.
                Session = ArchipelagoSessionFactory.CreateSession(host, port);

                // Attempts to log into the AP server as the Dandara game.
                var result = Session.TryConnectAndLogin(
                    "Dandara",
                    player,
                    ItemsHandlingFlags.AllItems,
                    password: string.IsNullOrEmpty(password) ? null : password
                );

                if (!result.Successful)
                {
                    MelonLogger.Error("Connection failed! Check settings.");
                    DataManager.LogActivity(
                        "APConnectFailed",
                        GetCurrentScene(),
                        player,
                        host + ":" + port);

                    Session = null;

                    if (_isReconnecting)
                        TryReconnect();

                    return;
                }

                MelonLogger.Msg("Connected to Archipelago!");
                DataManager.LogActivity(
                    "APConnected",
                    GetCurrentScene(),
                    player,
                    host + ":" + port);

                _reconnectAttempts = 0;
                _isReconnecting = false;

                // Gives the APServer class the live session so it can send and receive data.
                APServer.Init(Session);

                // Hooks socket events so this class can react to disconnects/errors/open.
                Session.Socket.SocketClosed += OnSocketClosed;
                Session.Socket.ErrorReceived += OnSocketError;
                Session.Socket.SocketOpened += OnSocketOpened;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Connection exception: " + ex.Message);
                DataManager.LogActivity(
                    "APConnectException",
                    GetCurrentScene(),
                    player,
                    ex.Message);

                Session = null;

                if (_isReconnecting)
                    TryReconnect();
            }
        }

        // Called when the socket closes.
        // If reconnect mode is active, this tries to reconnect automatically.
        private static void OnSocketClosed(string reason)
        {
            if (!_isReconnecting)
            {
                MelonLogger.Warning("Socket closed (manual), not reconnecting.");
                DataManager.LogActivity(
                    "APSocketClosed",
                    GetCurrentScene(),
                    _playerName.Value,
                    reason);
                return;
            }

            string host = _host.Value;
            int port = _port.Value;
            string player = _playerName.Value;

            MelonLogger.Warning("Connection lost to " + host + ":" + port + " as " + player);
            MelonLogger.Warning("Reason: " + reason);
            MelonLogger.Warning("Attempting auto-reconnect...");

            DataManager.LogActivity(
                "APConnectionLost",
                GetCurrentScene(),
                player,
                reason);

            APServer.Shutdown();
            Session = null;
            TryReconnect();
        }

        // Called when the AP socket reports an error.
        private static void OnSocketError(Exception ex, string message)
        {
            MelonLogger.Error("[AP Error] " + message);
            if (ex != null)
                MelonLogger.Error("Exception: " + ex.Message);

            DataManager.LogActivity(
                "APSocketError",
                GetCurrentScene(),
                _playerName.Value,
                ex != null ? ex.Message : message);
        }

        // Called when the socket opens successfully.
        private static void OnSocketOpened()
        {
            MelonLogger.Msg("[AP] Socket opened");
            DataManager.LogActivity(
                "APSocketOpened",
                GetCurrentScene(),
                _playerName.Value,
                "");
        }

        // Disconnects from AP and cleans up event handlers.
        public static void Disconnect(string reason)
        {
            if (!IsConnected)
            {
                MelonLogger.Warning("No active connection.");
                return;
            }

            string host = _host.Value;
            int port = _port.Value;
            string player = _playerName.Value;

            MelonLogger.Warning("Disconnecting from " + host + ":" + port + " as " + player + " | Reason: " + reason);

            try
            {
                Session.Socket.SocketClosed -= OnSocketClosed;
                Session.Socket.ErrorReceived -= OnSocketError;
                Session.Socket.SocketOpened -= OnSocketOpened;

                APServer.Shutdown();

                DataManager.LogActivity(
                    "APDisconnected",
                    GetCurrentScene(),
                    player,
                    reason);

                Session = null;
                MelonLogger.Msg("Disconnected.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Disconnect error: " + ex.Message);
            }
        }

        // Manual disconnect disables auto reconnect.
        public static void ManualDisconnect()
        {
            if (!IsConnected)
            {
                MelonLogger.Warning("No active connection.");
                return;
            }

            MelonLogger.Warning("Manual disconnect triggered");

            _isReconnecting = false;
            _reconnectAttempts = 0;

            string reason = "Manual disconnect";
            string host = _host.Value;
            int port = _port.Value;
            string player = _playerName.Value;

            MelonLogger.Warning("Connection lost to " + host + ":" + port + " as " + player);
            MelonLogger.Warning("Reason: " + reason);

            DataManager.LogActivity(
                "APManualDisconnect",
                GetCurrentScene(),
                player,
                reason);

            Session = null;
            APServer.Shutdown();

            MelonLogger.Msg("Disconnected. Auto-reconnect disabled.");
        }

        // Attempts reconnect until max attempts is reached.
        private static void TryReconnect()
        {
            if (!_isReconnecting)
                return;

            if (_reconnectAttempts >= MaxReconnectAttempts)
            {
                MelonLogger.Error("Max reconnect attempts reached. Press F5.");
                DataManager.LogActivity(
                    "APReconnectStopped",
                    GetCurrentScene(),
                    _playerName.Value,
                    "Max reconnect attempts reached");
                _isReconnecting = false;
                return;
            }

            _reconnectAttempts++;
            int left = MaxReconnectAttempts - _reconnectAttempts;

            MelonLogger.Warning("Reconnect attempt... (" + left + " left)");

            DataManager.LogActivity(
                "APReconnectAttempt",
                GetCurrentScene(),
                _playerName.Value,
                "Attempts left: " + left);

            Connect(true);
        }

        // Called every frame by Main.OnUpdate().
        // Handles hotkeys for config reload / connect / force disconnect.
        public static void OnUpdate()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // F1 reloads AP config from file.
            if (Input.GetKeyDown(KeyCode.F1))
                ReloadConfig();

            // Ctrl+Shift+F5 does a manual disconnect with no reconnect.
            if (ctrl && shift && Input.GetKeyDown(KeyCode.F5))
            {
                ManualDisconnect();
                return;
            }

            // F5 starts a normal connect attempt.
            if (Input.GetKeyDown(KeyCode.F5))
            {
                _reconnectAttempts = 0;
                _isReconnecting = true;
                Connect();
            }

            // F9 forces a test disconnect, then tries reconnect logic.
            if (Input.GetKeyDown(KeyCode.F9) && IsConnected)
            {
                MelonLogger.Warning("Forced drop (F9)");
                _isReconnecting = true;

                Disconnect("Forced test");
                TryReconnect();
            }
        }

        // Gets the current game scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
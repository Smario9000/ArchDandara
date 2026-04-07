// Server.cs
// Handles the server-side session logic for AP once connected.
// This class listens for received AP items and sends completed location checks.

using ArchDandara.Database;
using ArchDandara.Gamehook;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Helpers;
using MelonLoader;

namespace ArchDandara.Archipelago
{
    public static class APServer
    {
        // Stores the active Archipelago session passed in from APClient.
        private static ArchipelagoSession _session;

        // Public getter in case other classes need the active session.
        public static ArchipelagoSession Session
        {
            get { return _session; }
        }

        // Returns true if the AP session is active and the socket is connected.
        public static bool IsConnected
        {
            get
            {
                return _session != null &&
                       _session.Socket != null &&
                       _session.Socket.Connected;
            }
        }

        // Called after APClient connects successfully.
        // Registers handlers for incoming AP items.
        public static void Init(ArchipelagoSession session)
        {
            _session = session;

            MelonLogger.Msg("[AP][Server] Initializing handlers...");
            _session.Items.ItemReceived += OnItemReceived;
            MelonLogger.Msg("[AP][Server] Handlers registered");
        }

        // Cleans up event handlers and clears the session.
        public static void Shutdown()
        {
            if (_session == null)
                return;

            MelonLogger.Msg("[AP][Server] Shutting down...");
            _session.Items.ItemReceived -= OnItemReceived;
            _session = null;
        }

        // Called when AP sends this player an item.
        private static void OnItemReceived(ReceivedItemsHelper helper)
        {
            MelonLogger.Msg("[AP][Item] Item received event fired");

            try
            {
                // Logs the receive event to your mod files.
                DataManager.LogActivity(
                    "APItemReceived",
                    GetCurrentScene(),
                    "Archipelago",
                    "ReceivedItemsHelper event");

                MelonLogger.Msg("[AP][Item] Processing item (no direct access in this API)");

                // Temporary test behavior:
                // gives the player 50 money when any AP item arrives.
                APMoney.GiveMoney(50);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[AP][Item] ERROR: " + ex.Message);

                DataManager.LogActivity(
                    "APItemReceiveError",
                    GetCurrentScene(),
                    "Archipelago",
                    ex.Message);
            }
        }

        // Sends a completed Archipelago location check to the server.
        public static void SendLocationCheck(long locationId)
        {
            if (_session == null)
            {
                MelonLogger.Warning("[AP][Send] No session, cannot send check.");
                return;
            }

            try
            {
                MelonLogger.Msg("[AP][Send] Sending location check: " + locationId);

                DataManager.LogCheck(
                    "APSendLocationCheck",
                    GetCurrentScene(),
                    "Archipelago",
                    locationId.ToString(),
                    "");

                _session.Locations.CompleteLocationChecks(new long[] { locationId });

                MelonLogger.Msg("[AP][Send] SUCCESS location check: " + locationId);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[AP][Send] FAILED: " + ex.Message);

                DataManager.LogActivity(
                    "APSendLocationCheckError",
                    GetCurrentScene(),
                    locationId.ToString(),
                    ex.Message);
            }
        }

        // Gets the current room name for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
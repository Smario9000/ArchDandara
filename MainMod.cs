using System;
using UnityEngine;
using MelonLoader;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using MelonLoader.Logging;

[assembly: MelonInfo(typeof(ArchDandara.MainMod), "ArchDandara", "0.0.5", "Smores9000")]
[assembly: MelonGame("Long Hat House", "Dandara")]

namespace ArchDandara
{
    public class MainMod : MelonMod
    {
        /*public static ArchipelagoSession Session;

        private MelonPreferences_Category _category;
        private MelonPreferences_Entry<string> _hostEntry;
        private MelonPreferences_Entry<int> _portEntry;
        private MelonPreferences_Entry<string> _slotNameEntry;
        private MelonPreferences_Entry<string> _passwordEntry;
        public override void OnInitializeMelon()
        {
            // Initialize configuration
            _category = MelonPreferences.CreateCategory("DandaraArchipelago", "Dandara Archipelago Settings");
            _category.SetFilePath("UserData/DandaraArchipelago.cfg"); // Force specific file path
            _hostEntry = _category.CreateEntry("Host", "localhost");
            _portEntry = _category.CreateEntry("Port", 38281);
            _slotNameEntry = _category.CreateEntry("SlotName", "Player1");
            _passwordEntry = _category.CreateEntry("Password", "");

            // Force save immediately so the file appears
            _category.SaveToFile();

            LoggerInstance.Msg("Dandara Archipelago Mod Initialized!");
            LoggerInstance.Msg("Press F5 to Connect.");
            LoggerInstance.Msg("Press F1 to show settings.");

            // Start the console listener in a background thread
            // so we don't freeze the game.

        }

        public override void OnUpdate()
        {
            // Check for key presses every frame
            if (Input.GetKeyDown(KeyCode.F5))
            {
                LoggerInstance.Msg("F5 Pressed - Attempting Connection...");
                Connect();
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                LoggerInstance.Msg($"Current Settings: Host={_hostEntry.Value}, Port={_portEntry.Value}, Slot={_slotNameEntry.Value}");
            }
        }

        private void Connect()
        {
            string host = _hostEntry.Value;
            int port = _portEntry.Value;
            string uri = $"ws://{host}:{port}";

            try
            {
                Session = ArchipelagoSessionFactory.CreateSession(uri);
                Session.Items.ItemReceived += OnItemReceived;

                LoggerInstance.Msg($"Connecting to {uri}...");

                var result = Session.TryConnectAndLogin(
                    "Dandara",
                    _slotNameEntry.Value,
                    ItemsHandlingFlags.AllItems,
                    password: _passwordEntry.Value
                );

                if (result is LoginSuccessful)
                {
                    LoggerInstance.Msg($"Successfully connected to Archipelago as {_slotNameEntry.Value}!");
                }
                else if (result is LoginFailure failure)
                {
                    LoggerInstance.Error($"Failed to connect to Archipelago: {string.Join(", ", failure.Errors)}");
                }
            }
            catch (Exception ex)
            {
                LoggerInstance.Error($"An error occurred while connecting: {ex.Message}");
                LoggerInstance.Error(ex.StackTrace);
            }
        }

        private void OnItemReceived(Archipelago.MultiClient.Net.Helpers.ReceivedItemsHelper helper)
        {
            var item = helper.DequeueItem();
            string itemName = Session.Items.GetItemName(item.ItemId);
            LoggerInstance.Msg($"Received item: {itemName}");

            // TODO: Add logic here to actually give the item to the player in-game
        }*/
        // Shared/global objects
        // ------------------------
        // These static properties let other classes reference the central services easily:
        public static ArchDandaraConfig Config{ get; private set; }
        public static ArchDandaraAPConfig APConfig{ get; private set; }
        public static DoorJsonManager DoorJsonManager { get; private set; }
        public static DoorRandomizer DoorRandomizer { get; private set; }
        public static RoomDoorScanner RoomDoorScanner { get; private set; }
        public static MoneyPickupPatch MoneyPatch { get; private set; }
        // Harmony instance used to patch methods at runtime (namespace+id identifies the patcher)
        private HarmonyLib.Harmony _harmony;

        // Simple local enum controlling how our custom logger chooses color
        private enum LogColorMode { Normal, Color, RGB }
        private static readonly LogColorMode LOGMode = LogColorMode.RGB;

        // Fixed ColorARGB values from MelonLoader (used for pretty console output)
        private static readonly ColorARGB InfoColor = ColorARGB.Cyan;
        private static readonly ColorARGB WarningColor = ColorARGB.Yellow;
        private static readonly ColorARGB ErrorColor = ColorARGB.Red;
        private static readonly ColorARGB DebugColor = ColorARGB.Green;
        private static readonly ColorARGB DefaultColor = ColorARGB.White;

        private static float _rgbHue; // hue state for rainbow logging

        // Purpose:
        //  - Called once by MelonLoader when your mod is loaded into the game process.
        //  - Use this to create and initialize any global systems your mod needs.
        //
        // Key rules:
        //  - Keep heavy work off this thread if it could block. (Your code here does file I/O
        //    and initialization; those are fine as they are quick.)
        //  - Initialize systems in a deterministic order: config -> services -> optional subsystems -> patches.
        //
        // What this implementation does (step-by-step):
        //  1. Print a short startup message so you *know* the mod was discovered by MelonLoader.
        //  2. Initialize configuration (this creates the cfg file if missing and loads values).
        //  3. Load the Archipelago AP config (separate .cfg for AP settings).
        //  4. Initialize the DoorJsonManager (makes sure your JSON folder and file paths exist).
        //  5. Construct instances for Config and APConfig objects (so you can access them from other classes).
        //  6. Check config flags to selectively enable/disable logging and scanning — this prevents
        //     unnecessary work and spam when the user turned features off.
        //  7. Create the RoomDoorScanner only if scanning was enabled.
        //  8. Create and apply Harmony patches last. Doing patches last reduces the window where your
        //     patchable target code is unpatched but services are running.
        //
        // Why this order matters:
        //  - Config must be loaded early so features (like RoomDoorScanner) can query user preferences
        //  - DoorJsonManager should exist before the scanner so the scanner can immediately write door entries
        //  - Patches are applied last to avoid patching partially-initialized state
        //
        // If something fails here, MelonLoader will still continue loading other mods; log and handle exceptions
        // so users can diagnose issues (you already have logging calls which is perfect).

        public override void OnInitializeMelon()
        {
            _harmony = HarmonyInstance;
            // Small, colored startup message — confirms the mod entry executed.
            MelonLogger.Msg(DefaultColor, "[MainMod] ArchDandara Mod Loaded — Now with Full Documentation!");
            
            // ---- CONFIGURATION ----
            // Initialize the main config subsystem. This will ensure the file exists and load values.
            

            // Load Archipelago-specific config (separate file).
            ArchDandaraAPConfig.Init();
            ArchDandaraConfig.Init();
            // DoorJsonManager.Init() ensures the JSON folder/file are prepared.
            // Use the Init/Load pattern for static managers that require disk IO early.
            DoorJsonManager.Init();
            
            // Info about it DoorRandomizer
            DoorRandomizer.Init();
            MoneyPickupPatch.Init();
            // Helpful message so you can see the debug flag state loading early in the log.
            MelonLogger.Msg("[MainMod] Debug Flags loaded.");

            // Create instances of your object-oriented config wrappers so other classes can use them.
            Config = new ArchDandaraConfig();
            
            // For setting up the connection to the server
            APConfig = new ArchDandaraAPConfig();
            
            // Optional: if the user disabled archipelago debug logs, bail out early.
            // (Note: this returns from the whole method and prevents further initialization.)
            if (!ArchDandaraConfig.LogAPDebug)
            {
                MelonLogger.Msg("[MainMod] Logs for [Archipelago] Client is Off.");
                return;
            }

            // Build runtime services next.
            DoorJsonManager = new DoorJsonManager(); // ensures runtime instance is ready for scanner

            // If user turned off the DoorJsonManager logs, inform and continue (not fatal).
            if (!ArchDandaraConfig.LogDoorJsonManager)
            {
                MelonLogger.Msg("[MainMod] Logs for [DoorJsonManager] is Off.");
                // Note: we continue — logging being off does not disable the system itself.
            }
            
            // For using Randomizing the doors and where they will send you
            DoorRandomizer = new DoorRandomizer();
            
            // Enable DoorRandomizer only if config says so. This avoids unnecessary scene hooks.
            if (!ArchDandaraConfig.LogDoorRandomizer)
            {
                MelonLogger.Msg("[MainMod] Logs for [DoorRandomizer] is Off.");
                // Intentionally do not create the RoomDoorScanner instance.
                // We still create Harmony patches below (if you want patches only with scanner, gate those too).
                return;
            }
            // Create the scanner now that config and DoorJsonManager are ready.
            RoomDoorScanner = new RoomDoorScanner();
            // Enable RoomDoorScanner only if config says so. This avoids unnecessary scene hooks.
            if (!ArchDandaraConfig.EnableRoomScanning)
            {
                MelonLogger.Msg("[MainMod] RoomDoorScanner DISABLED by config");
                // Intentionally do not create the RoomDoorScanner instance.
                // We still create Harmony patches below (if you want patches only with scanner, gate those too).
                return;
            }
            // If room scanner logging is disabled — just note it.
            if (!ArchDandaraConfig.LogRoomDoorScanner)
            {
                MelonLogger.Msg("[MainMod] Logs for [RoomDoorScanner] is Off.");
                // Again: logging off doesn't disable functionality; it's about console noise.
            }

            MoneyPatch = new MoneyPickupPatch();
            // If room scanner logging is disabled — just note it.
            if (!ArchDandaraConfig.LogMoneyPatch)
            {
                MelonLogger.Msg("[MainMod] Logs for [MoneyPatch] is Off.");
                // Again: logging off doesn't disable functionality; it's about console noise.
            }
            // Apply the save blocking patches
            _harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
        }

        // =====================================================================================
        // Helper: PrintWithColor
        // =====================================================================================
        // Converts a message to a color (if RGB mode) and sends to MelonLogger.
        private static void PrintWithColor(string msg, ColorARGB baseColor)
        {
            if (LOGMode == LogColorMode.RGB)
            {
                baseColor = HueToStaticRainbow(_rgbHue);
                _rgbHue += 0.03f;
                if (_rgbHue > 1f) _rgbHue -= 1f;
            }

            if (LOGMode == LogColorMode.Normal) baseColor = DefaultColor;
            MelonLogger.Msg(baseColor, msg);
        }

        // Convert simplified hue to ColorARGB (chunked colors — simple and robust).
        private static ColorARGB HueToStaticRainbow(float hue)
        {
            hue = hue - (float) Math.Floor(hue);
            float zone = hue * 6f;
            if (zone < 1f) return ColorARGB.Red;
            if (zone < 2f) return ColorARGB.Magenta;
            if (zone < 3f) return ColorARGB.Blue;
            if (zone < 4f) return ColorARGB.Cyan;
            if (zone < 5f) return ColorARGB.Green;
            return ColorARGB.Yellow;
        }

        // =====================================================================================
        // Harmony patch: intercept Debug.Log(object)
        // =====================================================================================
        // We patch UnityEngine.Debug.Log(object) so we can re-route or filter in-game logs.
        // Guarded by your config flag (ArchDandaraConfig.LogDebugPatch).
        [HarmonyLib.HarmonyPatch(typeof(Debug), "Log", new[] { typeof(object) })]
        private static class DebugLogPatch
        {
            static void Prefix(object message)
            {
                if (!ArchDandaraConfig.LogDebugPatch)
                    return;

                string msg = message?.ToString() ?? "";
                string lower = msg.ToLowerInvariant();

                // Ignore known spam
                if (lower.Contains("[input mode]"))
                    return;

                bool interesting =
                    lower.Contains("item") ||
                    lower.Contains("pickup") ||
                    lower.Contains("collect") ||
                    lower.Contains("chest") ||
                    lower.Contains("open") ||
                    lower.Contains("treasure") ||
                    lower.Contains("upgrade") ||
                    lower.Contains("buy");

                // Always print normal logs if debug is on
                PrintWithColor($"[DebugLog] {msg}", DebugColor);

                if (!interesting)
                    return;

                // 🔥 IMPORTANT PART — STACK TRACE
                PrintWithColor("────────── 🔎 INTERESTING EVENT DETECTED 🔎 ──────────", DebugColor);

                try
                {
                    var stack = new System.Diagnostics.StackTrace(2, false);
                    foreach (var frame in stack.GetFrames())
                    {
                        var method = frame.GetMethod();
                        var type = method?.DeclaringType;

                        if (type == null)
                            continue;

                        // Skip Harmony / MelonLoader internals
                        string ns = type.Namespace ?? "";
                        if (ns.StartsWith("Harmony") || ns.StartsWith("MelonLoader"))
                            continue;

                        PrintWithColor(
                            $" → {type.FullName}.{method.Name}",
                            DebugColor
                        );
                    }
                }
                catch
                {
                    PrintWithColor("⚠ Failed to capture stack trace", DebugColor);
                }

                PrintWithColor("──────────────────────────────────────────────────────", DebugColor);
            }
        }
        // Small wrappers for structured logging
        private static void LogInfo(string msg) => PrintWithColor($"[INFO]  {msg}", InfoColor);
        private static void LogWarn(string msg) => PrintWithColor($"[WARN]  {msg}", WarningColor);
        private static void LogError(string msg) => PrintWithColor($"[ERROR] {msg}", ErrorColor);
        private static void LogDebug(string msg) => PrintWithColor($"[DEBUG] {msg}", DebugColor);
    }
}



// ====================================================================================================
//  ArchDandara — Fully Commented Mod File
//  This version contains MAXIMUM explanation for beginners and future maintainers.
//  Every system is documented: MelonLoader, Harmony, logging, HSV → RGB, patching, filtering, etc.
// ====================================================================================================

// This manager handles saving and loading door metadata to/from JSON.
// It is designed for Unity (Mono) + MelonLoader mods.
// The goal: Allow you to rewrite what door leads to what room by editing JSON.
// This version is clean, commented, and expandable.

using System;
using System.Collections.Generic;
using System.IO; // Core .NET types (string, Math, byte, etc.)
using MelonLoader;                // Main MelonLoader API (MelonMod, MelonInfo, logging system)
using HarmonyLib;                 // Harmony patching library used to intercept game functions
using MelonLoader.Logging;
using MelonLoader.Utils;
using UnityEngine;                // Unity game engine types (Debug.Log, GameObject, Time, etc.)
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// ====================================================================================================
//  MELONLOADER METADATA ATTRIBUTES
// ----------------------------------------------------------------------------------------------------
//  • MelonInfo tells MelonLoader what your mod is called, version, and who made it
//  • MelonGame restricts the mod so it only loads when the targeted game is running
// ====================================================================================================
//[assembly: MelonInfo(typeof(ArchDandara.MainMod), "ArchDandara", "0.0.1", "Smores9000")]
[assembly: MelonInfo(typeof(ArchDandara.MainMod), "ArchDandara", "0.0.3", "Smores9000")]
[assembly: MelonGame("Long Hat House", "Dandara")]

namespace ArchDandara
{
    public class MainMod : MelonMod
    {
        // ============================================================================================
        //  HARMONY — Runtime Code Patching
        // --------------------------------------------------------------------------------------------
        // Harmony allows us to modify existing game functions at runtime.
        // Example: patching UnityEngine.Debug.Log so all logs pass through our colored logger.
        // ============================================================================================
        private HarmonyLib.Harmony _harmony;

        // ============================================================================================
        //  LOGGING MODE ENUM — determines how logs are printed
        // --------------------------------------------------------------------------------------------
        // Normal → white only
        // Color  → fixed color per log level
        // RGB    → full rainbow (HSV cycling)
        // ============================================================================================
        private enum LogColorMode
        {
            Normal, // No colors, pure white
            Color, // Static color per category
            RGB // Smoothly changing hue per log
        }

        // Set current mode here (RGB = animated rainbow)
        private static readonly LogColorMode LOGMode = LogColorMode.RGB;

        // ============================================================================================
        //  FIXED COLOR SET — ColorARGB from MelonLoader (NOT System.ConsoleColor!)
        // --------------------------------------------------------------------------------------------
        //  ColorARGB uses 0–255 bytes per channel and works with MelonLogger.
        // ============================================================================================
        private static readonly ColorARGB InfoColor = ColorARGB.Cyan; // Bright cyan
        private static readonly ColorARGB WarningColor = ColorARGB.Yellow; // Warning yellow
        private static readonly ColorARGB ErrorColor = ColorARGB.Red; // Critical red
        private static readonly ColorARGB DebugColor = ColorARGB.Green; // Debug green
        private static readonly ColorARGB DefaultColor = ColorARGB.White; // Default white

        // ============================================================================================
        //  HSV HUE TRACKER — stores our current rainbow cycle value
        // --------------------------------------------------------------------------------------------
        // hue goes 0 → 1 and wraps around. This animates the rainbow color.
        // ============================================================================================
        private static float _rgbHue;



        // ============================================================================================
        //  MOD INITIALIZATION — runs ONE TIME when MelonLoader loads the mod
        // --------------------------------------------------------------------------------------------
        //  PERFECT for:
        //  • Applying Harmony patches
        //  • Loading config/settings
        //  • Initial logging
        // ============================================================================================
        public override void OnInitializeMelon()
        {
            // First message confirming the mod loaded successfully
            MelonLogger.Msg(DefaultColor, "ArchDandara Mod Loaded — Now with Full Documentation!");
            DoorJsonManager.Init();       // MUST run before scanner
            RoomDoorScanner.Init();       // Scanner registers events but does NOT manually call scene load
            // Create Harmony instance and apply all patches inside this assembly
            _harmony = new HarmonyLib.Harmony("com.you.archdandara");
            _harmony.PatchAll();
        }



        // ============================================================================================
        //  PrintWithColor — Core logging function for ALL colored logs
        // --------------------------------------------------------------------------------------------
        //  Behavior:
        //   • NORMAL → Always white
        //   • COLOR  → Uses provided static color
        //   • RGB    → Converts hue → RGB smoothly and increments hue
        // ============================================================================================
        private static void PrintWithColor(string msg, ColorARGB baseColor)
        {
            // If RGB mode: convert current hue → color
            if (LOGMode == LogColorMode.RGB)
            {
                baseColor = HueToStaticRainbow(_rgbHue);

                // Advance hue for next log
                _rgbHue += 0.03f; // Controls speed of rainbow
                if (_rgbHue > 1f)
                    _rgbHue -= 1f; // Wrap hue back to 0
            }

            // If NORMAL mode: override everything to white
            if (LOGMode == LogColorMode.Normal)
                baseColor = DefaultColor;

            // Final colored output to MelonLoader console
            MelonLogger.Msg(baseColor, msg);
        }

        private static ColorARGB HueToStaticRainbow(float hue)
        {
            hue = hue - (float)Math.Floor(hue);
            float zone = hue * 6f;

            if (zone < 1f) return ColorARGB.Red;
            if (zone < 2f) return ColorARGB.Magenta;
            if (zone < 3f) return ColorARGB.Blue;
            if (zone < 4f) return ColorARGB.Cyan;
            if (zone < 5f) return ColorARGB.Green;
            return ColorARGB.Yellow;
        }

        // ============================================================================================
        //  HSV → RGB COLOR CONVERSION (smooth rainbow)
        // --------------------------------------------------------------------------------------------
        // hue: 0–1
        // Returns: ColorARGB
        // --------------------------------------------------------------------------------------------
        // Explanation:
        //  HSV hue moves around the color wheel:
        //    0   = Red
        //    0.16 = Yellow
        //    0.33 = Green
        //    0.5  = Cyan
        //    0.66 = Blue
        //    0.83 = Magenta
        //    1.0 = Red again
        // ============================================================================================
        /*private static ColorARGB HsvToArgb(float hue)
        {
            hue = hue - (float)System.Math.Floor(hue); // Wrap to 0–1

            float r, g, b;
            float h = hue * 6f;                 // Convert to 0–6 (six color sectors)
            int sector = (int)System.Math.Floor(h);    // Sector index 0–5
            float f = h - sector;               // Fraction in sector

            float p = 0f;
            float q = 1f - f;
            float t = f;

            // Determine RGB per HSV sector
            switch (sector)
            {
                case 0: r = 1f; g = t;  b = p;  break;  // Red → Yellow
                case 1: r = q;  g = 1f; b = p;  break;  // Yellow → Green
                case 2: r = p;  g = 1f; b = t;  break;  // Green → Cyan
                case 3: r = p;  g = q;  b = 1f; break;  // Cyan → Blue
                case 4: r = t;  g = p;  b = 1f; break;  // Blue → Magenta
                default: r = 1f; g = p;  b = q;  break;  // Magenta → Red
            }

            // Convert float R/G/B (0–1) → byte (0–255)
            return new ColorARGB();
        }*/



        // ============================================================================================
        //  HARMONY PATCH — Intercepts UnityEngine.Debug.Log(object)
        // --------------------------------------------------------------------------------------------
        //  Behavior:
        //   • Any call to Debug.Log from the game is intercepted BEFORE executing
        //   • We can filter, rewrite, recolor, or entirely suppress logs
        // ============================================================================================
        [HarmonyPatch(typeof(Debug), "Log", new[] { typeof(object) })]
        private static class DebugLogPatch
        {
            static void Prefix(object message)
            {
                // Convert object → string safely
                string msg = message?.ToString() ?? "";

                // ------------------------------------------------------------------------------------
                // FILTER OUT NOISY LOG SPAM
                // (Game constantly prints controller/keyboard input mode spam — we remove it)
                // ------------------------------------------------------------------------------------
                if (msg.IndexOf("[INPUT MODE]", StringComparison.OrdinalIgnoreCase) >= 0)
                    return; // Skip entirely

                // ------------------------------------------------------------------------------------
                // Send message to our custom colorful logger
                // ------------------------------------------------------------------------------------
                PrintWithColor($"[DebugLogPatch] {msg}", DebugColor);
            }
        }


        // ============================================================================================
        //  OPTIONAL CLEAN WRAPPERS FOR LOG LEVELS
        // --------------------------------------------------------------------------------------------
        //  Use these anywhere in your code instead of PrintWithColor directly.
        // ============================================================================================
        private static void LogInfo(string msg) => PrintWithColor($"[INFO]  {msg}", InfoColor);
        private static void LogWarn(string msg) => PrintWithColor($"[WARN]  {msg}", WarningColor);
        private static void LogError(string msg) => PrintWithColor($"[ERROR] {msg}", ErrorColor);
        private static void LogDebug(string msg) => PrintWithColor($"[DEBUG] {msg}", DebugColor);
    }

    // =============================================================
    // ROOM + DOOR AUTO SCANNER
    // - Detects when the player enters any scene
    // - Scans all Door components
    // - Prints their name, position, and connected scene
    //
    // REQUIREMENTS:
    // - Game must have class "Door : SpawnPoint, IInteractable"
    // - Door must contain private field string _otherSideScene
    //
    // NOTES:
    // - This works entirely with MelonLoader's built-in logger
    // =============================================================
    /*public class RoomDoorScanner : MelonMod
    {
        // =============================================================
        // Called once on start. We hook the SceneManager event.
        // =============================================================
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("RoomDoorScanner initialized.");

            // Hook Unity’s sceneChanged callback.
            SceneManager.activeSceneChanged += OnSceneChanged;
        }


        // =============================================================
        // Triggered EVERY time a scene becomes active.
        // oldScene: the scene you just left
        // newScene: the scene you just entered
        // =============================================================
        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            MelonLogger.Msg("===========================================");
            MelonLogger.Msg($"Entered Scene / Room: {newScene.name}");
            MelonLogger.Msg("Now scanning for Door components...");
            MelonLogger.Msg("===========================================");

            ScanForDoors();
        }
        // Cached reference to the currently loaded scene name
        private static string _currentSceneName = "UNKNOWN";

        // Unity awakens all objects → good place to detect initial scene
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            // Save the scene name
            _currentSceneName = sceneName;

            // Print basic scene info
            MelonLogger.Msg("==========================================");
            MelonLogger.Msg("[ROOM ENTERED]");
            MelonLogger.Msg("Scene Name: " + sceneName);
            MelonLogger.Msg("==========================================");

            // Every time we enter a scene → scan all door objects
            ScanForDoors();
        }

        // =============================================================
        // Enumerates EVERY object in the scene to find Door components.
        //
        // Because Door inherits from SpawnPoint and IInteractable,
        // it is a MonoBehaviour that exists on a GameObject.
        //
        // We read:
        // - door.gameObject.name
        // - door.transform.position
        // - door._otherSideScene (private → use reflection)
        // =============================================================
        private void ScanForDoors()
        {
            // Older Unity versions do NOT support FindObjectsOfType<T>(true)
            // So we use Resources.FindObjectsOfTypeAll instead.
            // This returns *all* MonoBehaviours, including those disabled.
            MonoBehaviour[] allObjects = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

            foreach (var obj in allObjects)
            {
                if (obj == null)
                    continue;

                // Skip prefab ghosts and editor objects
                if (obj.hideFlags != HideFlags.None)
                    continue;

                // Skip objects NOT in the active scene
                if (!obj.gameObject.scene.isLoaded)
                    continue;

                // Skip Unity Editor preview objects or debugging duplicates
                if (obj.gameObject.name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Match by type name (Door)
                if (obj.GetType().Name == "Door")
                    LogDoor(obj);
            }
        }

        private string GetDoorDestination(object door)
        {
            Type t = door.GetType();

            string[] possibleNames =
            {
                "_otherSideScene",
                "otherSideScene",
                "m_otherSideScene",
                "doorDestination",
                "_destination",
                "destinationScene"
            };

            foreach (var n in possibleNames)
            {
                var f = t.GetField(n,
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public);

                if (f != null)
                {
                    var val = f.GetValue(door);
                    if (val != null)
                        return val.ToString();
                }
            }

            return "UNKNOWN";
        }

        // =============================================================
        // Logs data about an individual Door component.
        // Uses reflection to extract the private field _otherSideScene.
        // =============================================================
        private void LogDoor(MonoBehaviour door)
        {
            // Get GameObject name
            string objName = door.gameObject.name;

            // Get world position
            Vector3 pos = door.transform.position;

            // Use reflection to get private: string _otherSideScene
            var otherSideField = door.GetType().GetField("_otherSideScene",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            string connectedScene = GetDoorDestination(door);

            if (otherSideField != null)
            {
                object value = otherSideField.GetValue(door);
                if (value != null)
                    connectedScene = value.ToString();
            }

            // =============================================================
            // DEBUG: Dump all private fields so we can find the right one
            // =============================================================
            //var fields = door.GetType().GetFields(
            //    System.Reflection.BindingFlags.NonPublic |
            //    System.Reflection.BindingFlags.Instance |
            //    System.Reflection.BindingFlags.Public);

            //MelonLogger.Msg("  -- Dumping Private Fields --");
            //foreach (var f in fields)
            //{
            //    object v = f.GetValue(door);
            //    MelonLogger.Msg($"    {f.Name} = {v}");
            //}
            //MelonLogger.Msg("  -- End Dump --");
            // =============================================================
            // PRINT EVERYTHING
            // =============================================================

            //old code
            //MelonLogger.Msg($"Door Found: {objName}");
            //MelonLogger.Msg($" - Position: {pos.x}  {pos.y}  {pos.z}");
            //MelonLogger.Msg($" - Leads To Scene: {connectedScene}");
            //MelonLogger.Msg("-------------------------------------------");

            //new way
            // =============================================================
            // PRETTY OUTPUT FORMAT — Option B
            // Prints door info as a clean multi-line block
            // =============================================================
            MelonLogger.Msg("──────────────────────────────────────────");
            MelonLogger.Msg($" Door: {objName}");
            MelonLogger.Msg($" Position:");
            MelonLogger.Msg($"   X = {pos.x}");
            MelonLogger.Msg($"   Y = {pos.y}");
            MelonLogger.Msg($"   Z = {pos.z}");
            MelonLogger.Msg($" Leads To Scene: {connectedScene}");
            MelonLogger.Msg("──────────────────────────────────────────");
        }
    }*/

    public static class RoomDoorScanner 
    {
        // ===============================================================
        //  CONFIG FLAGS
        // ===============================================================

        // Enable or disable printing door information when a new room loads.
        private const bool DeepScanEnabled = true;

        // Keep track of the last scanned scene so we avoid duplicates.
        private static string _lastSceneName = "";
        
        public static void Init() 
        { 
            MelonLogger.Msg("[RoomDoorScanner] is Starting up"); 
            // Register proper MelonLoader callback instead
            MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded); 
        }
        // ===============================================================
        //  MELON LOADER HOOK — Runs when the game finishes loading a scene
        // ===============================================================
        private static void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                MelonLogger.Warning("[RoomDoorScanner] Scene name was NULL — skipping scan.");
                return;
            }

            MelonLogger.Msg($"[RoomDoorScanner] Scene Loaded: {sceneName}");
            MelonLogger.Msg("===========================================\n");

            if (sceneName == _lastSceneName)
                return;

            _lastSceneName = sceneName;

            if (DeepScanEnabled)
                ScanRoom(sceneName);
        }


        // ===============================================================
        //  MAIN DOOR SCANNER
        // ===============================================================
        // ===============================================================
        // MAIN DOOR SCANNER (robust, reflection-based — no compile-time Door type)
        // ===============================================================
        private static void ScanRoom(string sceneName)
        {
            MelonLogger.Msg("===========================================");
            MelonLogger.Msg($"    ROOM SCAN — {sceneName}");
            MelonLogger.Msg("===========================================");

            // ------------------------------------------------------------
            // Find ALL root objects in the current scene
            // ------------------------------------------------------------
            Scene activeScene = SceneManager.GetSceneByName(sceneName);
            var roots = activeScene.GetRootGameObjects();

            int doorsFound = 0;

            // ------------------------------------------------------------
            // Iterate through root objects and their children
            // NOTE: we do NOT use root.GetComponentsInChildren<Door>() because
            // the game's Door class may not be referenceable at compile time.
            // Instead we get all Components and check their runtime type name.
            // ------------------------------------------------------------
            foreach (var root in roots)
            {
                // Grab every Component in this root's hierarchy (includes disabled)
                Component[] comps = root.GetComponentsInChildren<Component>(true);

                foreach (var comp in comps)
                {
                    // safety
                    if (comp == null) continue;

                    // Skip things that clearly aren't game objects (just in case)
                    if (comp.gameObject == null) continue;

                    // Match by runtime type name — the game's Door class is named "Door"
                    var compType = comp.GetType();
                    if (!string.Equals(compType.Name, "Door", StringComparison.Ordinal))
                        continue; // not a Door -> skip

                    // We found a Door-like component
                    doorsFound++;

                    // ---------------------------
                    // Extract basic info (safe via Component)
                    // ---------------------------
                    string doorName = comp.gameObject.name;
                    Vector3 pos = comp.transform.position;

                    // ---------------------------
                    // Find the value for "other side" — try several common names
                    // The game uses a private field called "_otherSideScene" (string),
                    // but other builds/obfuscation may use a different name, so we try a list.
                    // ---------------------------
                    string leadsToScene = "UNKNOWN"; // default if we can't find it

                    // Try fields first (private/public)
                    string[] possibleFieldNames = new[]
                    {
                        "_otherSideScene",
                        "otherSideScene",
                        "m_otherSideScene",
                        "doorDestination",
                        "_destination",
                        "destinationScene"
                    };

                    foreach (var fname in possibleFieldNames)
                    {
                        var f = compType.GetField(fname,
                            System.Reflection.BindingFlags.NonPublic |
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public);
                        if (f != null)
                        {
                            object fval = f.GetValue(comp);
                            if (fval != null)
                            {
                                // found it — set and break
                                leadsToScene = fval.ToString();
                                break;
                            }
                        }
                    }

                    // If fields didn't find anything, try properties too (some code uses properties)
                    if (leadsToScene == "UNKNOWN")
                    {
                        foreach (var pname in possibleFieldNames)
                        {
                            var p = compType.GetProperty(pname,
                                System.Reflection.BindingFlags.NonPublic |
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public);
                            if (p != null)
                            {
                                object pval = p.GetValue(comp, null);
                                if (pval != null)
                                {
                                    leadsToScene = pval.ToString();
                                    break;
                                }
                            }
                        }
                    }

                    // ---------------------------
                    // PRETTY MULTILINE DUMP (keeps your current format)
                    // ---------------------------
                    MelonLogger.Msg("[RoomDoorScanner]──────────────────────────────────────────");
                    MelonLogger.Msg($" Door: {doorName}");
                    MelonLogger.Msg($" Position:");
                    MelonLogger.Msg($"   X = {pos.x}");
                    MelonLogger.Msg($"   Y = {pos.y}");
                    MelonLogger.Msg($"   Z = {pos.z}");
                    MelonLogger.Msg($" Leads To Scene: {leadsToScene}");
                    MelonLogger.Msg("[RoomDoorScanner]──────────────────────────────────────────");
                    // ----------------------------------------------------
                    // WRITE DOOR TO JSON DATABASE
                    // ----------------------------------------------------
                    DoorRecord entry = new DoorRecord
                    {
                        doorName = doorName,
                        sceneName = sceneName,
                        otherSideScene = leadsToScene,
                        fakeSpawnID = "",
                        
                        posX = pos.x,
                        posY = pos.y,
                        posZ = pos.z
                    };
                    MelonLogger.Msg("===========================================\n");
                    MelonLogger.Msg("[DoorJsonManager] DoorJsonManager Updated");
                    DoorJsonManager.AddOrUpdateDoor(entry);
                    MelonLogger.Msg("===========================================\n");
                }
            }

            MelonLogger.Msg($"[RoomDoorScanner] Total Doors Found: {doorsFound}");
            // Save JSON after finishing this room
            DoorJsonManager.Save();
            MelonLogger.Msg("[DoorJsonManager] DoorJsonManager Saved");
            MelonLogger.Msg("===========================================\n");

        }
    }

    // ------------------------------------------------------------
    // Data model for a single door entry.
    // ------------------------------------------------------------
    //[Serializable]
    /*public class DoorEntry
    {
        // Name of the door GameObject
        [FormerlySerializedAs("DoorName")] public string doorName;


        // Name of the scene/room this door belongs to
        [FormerlySerializedAs("LocatedInScene")]
        public string locatedInScene;


        // Name of the scene this door leads to
        [FormerlySerializedAs("LeadsToScene")] public string leadsToScene;


        // Position of the door (for mapping)
        [FormerlySerializedAs("PosX")] public float posX;
        [FormerlySerializedAs("PosY")] public float posY;
        [FormerlySerializedAs("PosZ")] public float posZ;
    }*/

    // ------------------------------------------------------------
    // Container for storing multiple doors.
    // ------------------------------------------------------------
    [Serializable]
    public class DoorDatabase
    {
        [SerializeField] public List<DoorRecord> doors = new List<DoorRecord>();
    }

    [Serializable]
    public class DoorRecord
    {
        [SerializeField] public string sceneName;
        [SerializeField] public string doorName;
        [SerializeField] public string otherSideScene;
        [SerializeField] public string fakeSpawnID;
        // Position of the door (for mapping)
        [SerializeField] public float posX;
        [SerializeField] public float posY;
        [SerializeField] public float posZ;
    }

    public static class DoorJsonManager
    {
        private static string _jsonDirectory;
        private static string _jsonFile;
        private static DoorDatabase _database = new DoorDatabase();

        public static void Init()
        {
            // Correct: Build folder path only
            MelonLogger.Msg("[DoorJsonManager] Initializing...");
            _jsonDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "Dandara_Doors");
            // Ensure folder exists
            if (!Directory.Exists(_jsonDirectory))
                Directory.CreateDirectory(_jsonDirectory);
            // Correct: Build full file path
            _jsonFile = Path.Combine(_jsonDirectory, "door_database.json");

            // Ensure directory exists
            if (!Directory.Exists(_jsonDirectory))
                Directory.CreateDirectory(_jsonDirectory);

            MelonLogger.Warning($"[DoorJsonManager] JSON file: {_jsonFile}");
            MelonLogger.Warning($"[DoorJsonManager] Directory: {_jsonDirectory}");

            Load();
        }

        private static void Load()
        {
            if (!File.Exists(_jsonFile))
            {
                MelonLogger.Msg("[DoorJsonManager] No JSON found — creating new DB.");
                _database = new DoorDatabase();
                Save();
                return;
            }

            try
            {
                string json = File.ReadAllText(_jsonFile);

                // Unity JSON REQUIREMENT: wrapper object MUST NOT be null
                var loaded = JsonUtility.FromJson<DoorDatabase>(json);
                _database = loaded ?? new DoorDatabase();

                MelonLogger.Msg("[DoorJsonManager] Loaded door database.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Loading: " + e);
                _database = new DoorDatabase();
            }
        }

        public static void Save()
        {
            if (string.IsNullOrEmpty(_jsonFile))
            {
                MelonLogger.Error("[DoorJsonManager] ERROR: Save() called before Init()!");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(_database, true);
                File.WriteAllText(_jsonFile, json);
                MelonLogger.Msg("[DoorJsonManager] Saved database.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Saving: " + e);
            }
        }

        public static void AddOrUpdateDoor(DoorRecord entry)
        {
            DoorRecord rec = new DoorRecord
            {
                sceneName      = entry.sceneName,
                doorName       = entry.doorName,
                otherSideScene = entry.otherSideScene,
                fakeSpawnID    = "",

                posX = entry.posX,
                posY = entry.posY,
                posZ = entry.posZ
            };

            _database.doors.RemoveAll(d =>
                d.sceneName == rec.sceneName &&
                d.doorName == rec.doorName);

            _database.doors.Add(rec);
            Save();
        }
    }
}
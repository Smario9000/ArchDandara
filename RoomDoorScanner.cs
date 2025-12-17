//RoomDoorScanner.cs

using System.Linq;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArchDandara
{
    /// ============================================================================================
    ///  ROOM DOOR SCANNER
    /// --------------------------------------------------------------------------------------------
    ///  PURPOSE:
    ///      • Watches Unity scene loading and scans the entire hierarchy for Door components.
    ///      • Extracts door information (names, positions, destinations).
    ///      • Sends results to DoorJsonManager → JSON database.
    ///
    ///  This file is the largest "logic" class in your mod.
    ///  It is responsible for gathering ALL door metadata in the entire game world.
    ///
    ///  STYLE:
    ///      • First-time explanations = Option A (Training Manual)
    ///      • Repeated concepts      = Option B (Professional Developer)
    /// ============================================================================================
    public class RoomDoorScanner : MelonLogger
    {
        // ============================================================================================
        // LOGGING METHOD
        // --------------------------------------------------------------------------------------------
        // Purpose:
        //   - Your mod uses boolean flags in ArchDandaraConfig to enable/disable logging categories.
        //   - This Print() wrapper is used so every message automatically prefixes "[RoomDoorScanner]"
        //   - This keeps logs clean and easy to filter when debugging.
        //
        // Behavior:
        //   • level = 1 → Msg  (normal white text)
        //   • level = 2 → Warning (yellow)
        //   • level = 3 → Error (red)
        //
        // If LogRoomDoorScanner = false, nothing prints.
        // ============================================================================================
        private static void Print(string msg, int level = 1)
        {
            if (!ArchDandaraConfig.LogRoomDoorScanner)
                return;

            switch (level)
            {
                case 1: Msg("[RoomDoorScanner] " + msg); break;
                case 2: Warning("[RoomDoorScanner] " + msg); break;
                case 3: Error("[RoomDoorScanner] " + msg); break;
            }
        }

        // ============================================================================================
        // INTERNAL FLAGS
        // --------------------------------------------------------------------------------------------
        // DeepScanEnabled:
        //   • Controls whether the scanner performs a "full hierarchy sweep"
        //   • When TRUE → walks all root objects + their children to find Door components
        //   • You may later expand this to add other scan modes
        // ============================================================================================
        private const bool DeepScanEnabled = true;

        // Professional-level comment:
        private static string _lastSceneName = ""; // prevents double-scan of the same scene
        
        // ============================================================================================
        // INIT — Called ONE TIME from DoorJsonManager.Init()
        // --------------------------------------------------------------------------------------------
        // Why subscribe here instead of constructor?
        //   - MelonLoader mods typically use Init() or OnInitializeMelon() for setup.
        //   - Unity scene callbacks don't work until AFTER the mod is loaded.
        //
        // MelonEvents.OnSceneWasLoaded.Subscribe(...)
        //   - This attaches our OnSceneWasLoaded() function to Unity's scene change event.
        //   - Every time the game loads a new room, our scanner runs.
        //
        // Behavior:
        //   • If config disabling scanning → no subscription occurs.
        //   • If scanning enabled → SceneLoaded events start firing.
        // ============================================================================================
        public static void Init()
        {
            Print("is Starting up");
            MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded);
        }
        
        // ============================================================================================
        // ON SCENE LOAD — Called automatically on every Unity scene change
        // --------------------------------------------------------------------------------------------
        // Why does this run?
        //   - Because Init() subscribed this function to MelonEvents.OnSceneWasLoaded.
        //
        // What happens here?
        //   • Validate the scene name (protect against nulls)
        //   • Deduplicate events using _lastSceneName
        //   • Trigger a scan of the new scene
        //
        // Notes:
        //   - Unity sometimes loads internal scenes that have no meaningful data.
        //   - All output is gated behind your logging flags.
        // ============================================================================================
        private static void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "MainMenu" ||
                sceneName == "LoadingScreen" ||
                sceneName == "InitGame")
            {
                Print($"Skipping non-gameplay scene: {sceneName}", 2);
                return;
            }
            
            if (string.IsNullOrEmpty(sceneName))
            {
                Print("Scene name was NULL — skipping scan.", 2);
                return;
            }

            // Professional-level colored logging (delegate to MainMod printer)
            Print($"Scene Loaded: {sceneName}");

            // Prevent repeated scans
            if (sceneName == _lastSceneName)
                return;

            _lastSceneName = sceneName;

            // Scan only if full scanning is allowed
            if (DeepScanEnabled)
                ScanRoom(sceneName);
        }

        // ============================================================================================
        // SCAN ROOM — Extracts Door information from scene hierarchy
        // --------------------------------------------------------------------------------------------
        // How do we find doors in Unity without referencing the game's source code?
        //   - We walk through EVERY GameObject in the scene.
        //   - We check every Component on every object.
        //   - If the component's Type.Name == "Door", we treat it as a Door.
        //
        // Why not use GetComponent<Door>()?
        //   • Because the game's Door class is not available at compile time.
        //   • Using Type.Name avoids needing a reference to game assemblies.
        //
        // What data do we gather?
        //   • DoorName (GameObject name)
        //   • Position (X/Y/Z)
        //   • OtherSideScene (private field "_otherSideScene" or others)
        //
        // After scanning:
        //   • Every door is turned into a DoorRecord
        //   • DoorJsonManager.AddOrUpdateDoor writes it to JSON
        // ============================================================================================
        private static void ScanRoom(string sceneName)
        {
            Print("===========================================");
            Print($"        ROOM SCAN — {sceneName}");
            Print("===========================================");

            // Step 1 — Get Unity Scene object
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                Print("Invalid scene — skipping scan.", 2);
                return;
            }

            var roots = scene.GetRootGameObjects();
            if (roots == null || roots.Length == 0)
            {
                Print("Scene has no root objects — skipping.", 2);
                return;
            }

            int doorsFound = 0;

            // Step 2 — Walk all GameObject trees
            foreach (var root in roots)
            {
                // Get ALL components under this root (even inactive)
                Component[] comps = root.GetComponentsInChildren<Component>(true);

                foreach (var comp in comps)
                {
                    if (comp == null) continue;
                    if (comp.gameObject == null) continue;

                    var compType = comp.GetType();
                    string spawnID = "";
                    string fakeSpawnID = "";
                    
                    // Identify "Door" by name only (best method for obfuscated builds)
                    if (compType.Name != "Door")
                        continue;

                    doorsFound++;

                    // --------------------------
                    // Extract door data
                    // --------------------------
                    string doorName = comp.gameObject.name;
                    Vector3 pos = comp.transform.position;

                    // Attempt to extract destination from known private fields
                    string leadsToScene = "UNKNOWN";

                    string[] possibleNames =
                    {
                        "_otherSideScene",
                        "otherSideScene",
                        "m_otherSideScene",
                        "doorDestination",
                        "_destination",
                        "destinationScene"
                    };
                    
                    // Try fields first, then properties.
                    foreach (var fname in possibleNames)
                    {
                        var f = compType.GetField(fname,
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic);

                        if (f?.GetValue(comp) is string fval)
                        {
                            leadsToScene = fval;
                            break;
                        }
                    }

                    if (leadsToScene == "UNKNOWN")
                    {
                        foreach (var pname in possibleNames)
                        {
                            var p = compType.GetProperty(pname,
                                System.Reflection.BindingFlags.Instance |
                                System.Reflection.BindingFlags.Public |
                                System.Reflection.BindingFlags.NonPublic);

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
                    // --------------------------
                    // Extract Spawn IDs
                    // --------------------------
                    foreach (var f in compType.GetFields(
                                 System.Reflection.BindingFlags.Instance |
                                 System.Reflection.BindingFlags.Public |
                                 System.Reflection.BindingFlags.NonPublic))
                    {
                        object val = f.GetValue(comp);
                        Print($"[FIELD] {f.Name} = {val}");
                    }
                    
                    var spawnField = compType.GetField("spawnID",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (spawnField != null)
                    {
                        object val = spawnField.GetValue(comp);
                        spawnID = val?.ToString() ?? "";
                        Print($"Raw spawnID value type: {val?.GetType().FullName}");
                        Print($"Raw spawnID value: {val}");
                    }

                    var fakeSpawnField = compType.GetField("fakeSpawnID",
                        System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.NonPublic);

                    if (fakeSpawnField != null)
                    {
                        object val = fakeSpawnField.GetValue(comp);
                        fakeSpawnID = val?.ToString() ?? "";
                        Print($"Raw fakeSpawn value type: {val?.GetType().FullName}");
                        Print($"Raw fakeSpawn value: {val}");
                    }
                    // --------------------------
                    // Printed summary
                    // --------------------------
                    Print("──────────────────────────────────────────");
                    Print($" Door: {doorName}");
                    Print($" SpawnID: {spawnID}");
                    Print($" FakeSpawnID: {fakeSpawnID}");
                    Print($" Position:");
                    Print($"   X = {pos.x}");
                    Print($"   Y = {pos.y}");
                    Print($"   Z = {pos.z}");
                    Print($" Leads To Scene: {leadsToScene}");
                    Print("──────────────────────────────────────────");

                    // --------------------------
                    // Write to JSON database
                    // --------------------------
                    DoorJsonManager.Print("DoorJsonManager Updated");
                    DoorJsonManager.AddOrUpdateDoor(new DoorRecord
                    {
                        DoorName = doorName,
                        SceneName = sceneName,
                        OtherSideScene = leadsToScene,
                        SpawnID = spawnID,
                        FakeSpawnID = fakeSpawnID,
                        PosX = pos.x,PosY = pos.y,PosZ = pos.z
                    });
                }
            }

            // Final output
            Print($"Total Doors Found: {doorsFound}");
            DoorJsonManager.PrintJsonToLog();
            Print("===========================================\n");
        }
    }
}
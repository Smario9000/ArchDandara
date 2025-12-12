//RoomDoorScanner.cs

using System;
using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArchDandara
{
    public class RoomDoorScanner 
    {
        // ===============================================================
        //  CONFIG FLAGS
        // ===============================================================

        // Enable or disable printing door information when a new room loads.
        private const bool DeepScanEnabled = true;

        // Keep track of the last scanned scene so we avoid duplicates.
        private static string _lastSceneName = "";
        
        public RoomDoorScanner() 
        { 
            MelonLogger.Msg("[RoomDoorScanner] is Starting up"); 
            // Register proper MelonLoader callback instead
            MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneWasLoaded); 
        }
        // ===============================================================
        //  MELON LOADER HOOK — Runs when the game finishes loading a scene
        // ===============================================================
        private void OnSceneWasLoaded(int buildIndex, string sceneName)
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
        public void ScanRoom(string sceneName)
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
                    var entry = new DoorRecord
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
                    MainMod.DoorJsonManager.AddOrUpdateDoor(entry);
                    MelonLogger.Msg("===========================================\n");
                }
            }

            MelonLogger.Msg($"[RoomDoorScanner] Total Doors Found: {doorsFound}");
            // Print JSON ONLY ONCE PER SCENE
            MainMod.DoorJsonManager.PrintJsonToLog();
            MelonLogger.Msg("===========================================\n");

        }
    }
}
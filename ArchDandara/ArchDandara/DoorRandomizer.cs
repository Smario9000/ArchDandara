// DoorRandomizer.cs

// Responsible ONLY for modifying where doors send the player.
// Reads data from DoorJsonManager and applies it to live Door objects.
// This class does NOT scan doors.
// This class does NOT write JSON.
// This class does NOT manage scenes.
// Single responsibility: door destination override.

using MelonLoader;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Reflection;
using System.Collections;

namespace ArchDandara
{
    public class DoorRandomizer : MelonLogger
    {
        // ============================================================
        // LOGGING
        // ============================================================
        private static void Print(string msg, int level = 1)
        {
            if (!ArchDandaraConfig.LogDoorRandomizer)
                return;

            switch (level)
            {
                case 1: Msg("[DoorRandomizer] " + msg); break;
                case 2: Warning("[DoorRandomizer] " + msg); break;
                case 3: Error("[DoorRandomizer] " + msg); break;
            }
        }

        // ============================================================
        // INITIALIZATION
        // ============================================================
        public static void Init()
        {
            Print("Initializing...");
            MelonEvents.OnSceneWasLoaded.Subscribe(OnSceneLoaded);
        }

        // ============================================================
        // SCENE LOAD HANDLER
        // ============================================================
        private static void OnSceneLoaded(int buildIndex, string sceneName)
        {
            if (!ArchDandaraConfig.LogDoorRandomizer)
                return;

            Print($"Applying door randomization for scene: {sceneName}");
            MelonCoroutines.Start(ApplyDoorOverridesDelayed(sceneName));
        }
        private static IEnumerator ApplyDoorOverridesDelayed(string sceneName)
        {
            // Let Unity finish initializing scene objects
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            ApplyDoorOverrides(sceneName);
        }
        // ============================================================
        // CORE LOGIC
        // ============================================================
        private static void ApplyDoorOverrides(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid())
            {
                Print("Scene invalid — aborting", 2);
                return;
            }

            foreach (var root in scene.GetRootGameObjects())
            {
                var doors = root.GetComponentsInChildren<Component>(true);

                foreach (var comp in doors)
                {
                    if (comp == null) continue;
                    if (comp.GetType().Name != "Door") continue;

                    TryOverrideDoor(comp, sceneName);
                }
            }
        }

        // ============================================================
        // DOOR OVERRIDE
        // ============================================================
        private static void TryOverrideDoor(Component doorComponent, string currentScene)
        {
            string doorName = doorComponent.gameObject.name;

            var entry = DoorJsonManager
                .GetDoorRecord(currentScene, doorName);

            if (entry == null)
                return;

            Type doorType = doorComponent.GetType();
            
            // Skip doors currently interacting with player
            var enteringPlayerField = doorType.GetField(
                "_enteringPlayer",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (enteringPlayerField?.GetValue(doorComponent) != null)
            {
                Print($"Skipping active door '{doorName}' (player interacting)");
                return;
            }
            
            var leavingPlayerField = doorType.GetField(
                "_leavingPlayer",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (leavingPlayerField?.GetValue(doorComponent) != null)
                return;
            // Override _otherSideScene.
            FieldInfo otherSideSceneField = null;
            Type searchType = doorType;

            while (searchType != null)
            {
                otherSideSceneField = searchType.GetField(
                    "_otherSideScene",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
                );

                if (otherSideSceneField != null)
                    break;

                searchType = searchType.BaseType;
            }

            if (otherSideSceneField == null)
            {
                Print($"_otherSideScene not found on {doorType.FullName}", 3);
                return;
            }

            string original = (string)otherSideSceneField.GetValue(doorComponent);
            otherSideSceneField.SetValue(doorComponent, entry.OtherSideScene);

            Print($"Door '{doorName}' redirected: {original} → {entry.OtherSideScene}");

            // ------------------------------------------------------------
            // Override fakeSpawnID (THIS IS THE NEW PART)
            // ------------------------------------------------------------
            if (!string.IsNullOrEmpty(entry.FakeSpawnID))
            {
                FieldInfo spawnField = doorType.GetField(
                    "spawnID",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );

                if (spawnField != null)
                {
                    if (Enum.IsDefined(spawnField.FieldType, entry.FakeSpawnID))
                    {
                        object parsedSpawn = Enum.Parse(spawnField.FieldType, entry.FakeSpawnID);
                        spawnField.SetValue(doorComponent, parsedSpawn);

                        Print($"Spawn override applied for '{doorName}' → {entry.FakeSpawnID}");
                    }
                    else
                    {
                        Print($"Invalid SpawnID '{entry.FakeSpawnID}' — skipping override", 2);
                    }
                }
                else
                {
                    Print("spawnID field not found on Door", 3);
                }
            }
        }
    }
}

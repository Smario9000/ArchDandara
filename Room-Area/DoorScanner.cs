// DoorScanner.cs
// Runs room scans after transitions and collects all doors in the active room.

using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using ArchDandara.Database;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    [HarmonyPatch(typeof(GameManager), "OnTransitionEnded")]
    public static class RoomScanPatch
    {
        // Used to ignore immediate duplicate scans after the same room load.
        private static string _lastScannedScene = "";
        private static int _lastScannedFrame = -9999;

        private static void Postfix()
        {
            string scene = GetCurrentScene();

            // Skips same-scene scans that happen almost instantly.
            if (scene == _lastScannedScene && Time.frameCount - _lastScannedFrame <= 1)
            {
                DebugLogger.Log("Room scan ignored duplicate: " + scene);
                return;
            }

            _lastScannedScene = scene;
            _lastScannedFrame = Time.frameCount;

            string meta = DataManager.GetRoomMetaText(scene);

            MelonLogger.Msg("[RoomScan] Scene visible: " + scene + (string.IsNullOrEmpty(meta) ? "" : " | " + meta));

            // Runs all room scanners after a transition ends.
            DoorScanner.Scan();
            ChestScanner.Scan();
            NPCScanner.Scan();
            SoulScanner.Scan();
            StoryEventScanner.Scan();
            ShopUpgradeScanner.Scan();
        }

        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }

    public static class DoorScanner
    {
        // Finds all doors in the current room.
        public static void Scan()
        {
            try
            {
                Door[] doors = Object.FindObjectsOfType<Door>();

                if (doors == null || doors.Length == 0)
                {
                    MelonLogger.Warning("[DoorScanner] No doors found");
                    return;
                }

                List<Door> validDoors = new List<Door>();

                for (int i = 0; i < doors.Length; i++)
                {
                    Door door = doors[i];
                    if (door == null)
                        continue;

                    validDoors.Add(door);

                    string destination = !string.IsNullOrEmpty(door._otherSideScene) ? door._otherSideScene : "";
                    string destinationMeta = DataManager.GetRoomMetaText(destination);

                    // Extra gate logging for lockable doors.
                    if (door is LockeableDoor lockDoor)
                    {
                        bool unlocked = lockDoor.IsUnlocked();
                        string conditionalInfo = lockDoor._isLocked != null
                            ? "Conditional=" + lockDoor._isLocked.GetType().Name
                            : "Conditional=None";

                        DataManager.LogActivity(
                            "DoorGate",
                            GetCurrentScene(),
                            door.name != null ? door.name : "Door",
                            "Dest=" + destination +
                            " | Unlocked=" + unlocked +
                            " | " + conditionalInfo +
                            (string.IsNullOrEmpty(destinationMeta) ? "" : " | " + destinationMeta));
                    }

                    // Optional debug print for door position/rotation.
                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = door.transform.position;
                        float rotZ = door.transform.eulerAngles.z;

                        DebugLogger.Log(
                            "Door -> " +
                            (door.name != null ? door.name : "Door") + " -> " +
                            destination + " -> (" +
                            pos.x + "," + pos.y + "," + rotZ + ")" +
                            (string.IsNullOrEmpty(destinationMeta) ? "" : " | " + destinationMeta));
                    }
                }

                MelonLogger.Msg("[DoorScanner] Found doors: " + validDoors.Count);
                DoorLogger.LogRoom(validDoors);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[DoorScanner] Scan failed: " + ex.Message);
            }
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
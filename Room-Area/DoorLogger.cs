// DoorLogger.cs
// Saves the current room's door data and writes a simple room-scan activity log.

using System.Collections.Generic;
using MelonLoader;
using ArchDandara.Database;

namespace ArchDandara.Room_Area
{
    public static class DoorLogger
    {
        // Saves all doors found in the current room.
        public static void LogRoom(List<Door> doors)
        {
            if (doors == null || doors.Count == 0)
                return;

            try
            {
                string currentScene = GetCurrentScene();

                DataManager.SaveRoom(currentScene, doors);

                // Adds a room-scan activity line with optional map metadata.
                string meta = DataManager.GetRoomMetaText(currentScene);
                DataManager.LogActivity(
                    "RoomScanned",
                    currentScene,
                    "DoorLogger",
                    "Doors=" + doors.Count + (string.IsNullOrEmpty(meta) ? "" : " | " + meta));
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[DoorLogger] Failed: " + ex.Message);
            }
        }

        // Gets the current scene safely.
        private static string GetCurrentScene()
        {
            try
            {
                var gm = PersistentSingleton<GameManager>.instance;
                return gm != null ? gm.GetCurrentScene() : "UNKNOWN";
            }
            catch
            {
                return "ERROR";
            }
        }
    }
}
// ChestLogger.cs
// Filters found chest objects and saves valid chest data for the current room.

using System.Collections.Generic;
using ArchDandara.Database;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class ChestLogger
    {
        // Tracks chest instance IDs already seen for debug messages.
        private static readonly HashSet<int> ScannedChests = new HashSet<int>();

        // Saves valid chest data for the room.
        public static void LogChests(List<PowerupInteractable> list)
        {
            if (list == null || list.Count == 0)
                return;

            List<PowerupInteractable> validChests = new List<PowerupInteractable>();

            for (int i = 0; i < list.Count; i++)
            {
                PowerupInteractable chest = list[i];
                if (chest == null) continue;

                string lowerName = chest.name != null ? chest.name.ToLower() : "";
                if (!lowerName.Contains("chest")) continue;

                validChests.Add(chest);
            }

            if (validChests.Count == 0)
                return;

            string scene = GetCurrentScene();

            DataManager.SaveRoomChests(scene, validChests);

            // Prints debug info only the first time each chest is seen.
            for (int i = 0; i < validChests.Count; i++)
            {
                PowerupInteractable chest = validChests[i];
                int id = chest.GetInstanceID();

                if (ScannedChests.Add(id))
                {
                    DebugLogger.Log("Chest logged: " + chest.name + " in " + scene);
                }
            }
        }

        // Gets the current scene for saving.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN";
        }
    }
}
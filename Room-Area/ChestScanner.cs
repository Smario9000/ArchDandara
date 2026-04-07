// ChestScanner.cs
// Scans the current room for PowerupInteractable objects and passes them to ChestLogger.

using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class ChestScanner
    {
        public static void Scan()
        {
            try
            {
                // Finds all PowerupInteractable objects currently loaded.
                PowerupInteractable[] chests = Object.FindObjectsOfType<PowerupInteractable>();
                List<PowerupInteractable> foundChests = new List<PowerupInteractable>();

                for (int i = 0; i < chests.Length; i++)
                {
                    PowerupInteractable chest = chests[i];
                    if (chest == null)
                        continue;

                    foundChests.Add(chest);

                    // Optional debug print with position.
                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = chest.transform.position;

                        DebugLogger.Log(
                            "Chest -> " +
                            chest.name + " -> " +
                            chest.GetType().Name + " -> (" +
                            pos.x + "," + pos.y + "," + pos.z + ")");
                    }
                }

                MelonLogger.Msg("[ChestScanner] Found chests: " + foundChests.Count);
                ChestLogger.LogChests(foundChests);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ChestScanner] Scan failed: " + ex.Message);
            }
        }
    }
}
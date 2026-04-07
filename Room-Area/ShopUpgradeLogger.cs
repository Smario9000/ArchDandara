// ShopUpgradeLogger.cs
// Saves shop-upgrade proxy objects for the current room.

using System.Collections.Generic;
using ArchDandara.Database;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class ShopUpgradeLogger
    {
        // Saves the list of found PowerupManagerProxy objects.
        public static void LogShopUpgrades(List<PowerupManagerProxy> list)
        {
            if (list == null || list.Count == 0)
                return;

            string scene = GetCurrentScene();

            DataManager.SaveRoomShopUpgrades(scene, list);

            // Optional debug print for each upgrade proxy found.
            for (int i = 0; i < list.Count; i++)
            {
                PowerupManagerProxy upgrade = list[i];
                if (upgrade == null) continue;

                DebugLogger.Log(
                    "Shop upgrade logged: " +
                    upgrade.gameObject.name + " -> " +
                    upgrade.toUnlock + " -> " +
                    upgrade.uniqueID + " in " + scene);
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
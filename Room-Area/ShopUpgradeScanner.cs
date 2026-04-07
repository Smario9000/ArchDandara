// ShopUpgradeScanner.cs
// Scans the current room for PowerupManagerProxy objects that look like real shop upgrades.

using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class ShopUpgradeScanner
    {
        public static void Scan()
        {
            try
            {
                // Finds all PowerupManagerProxy objects currently loaded.
                PowerupManagerProxy[] proxies = Object.FindObjectsOfType<PowerupManagerProxy>();
                List<PowerupManagerProxy> foundUpgrades = new List<PowerupManagerProxy>();
                HashSet<int> seen = new HashSet<int>();

                for (int i = 0; i < proxies.Length; i++)
                {
                    PowerupManagerProxy proxy = proxies[i];
                    if (proxy == null || proxy.gameObject == null)
                        continue;

                    // Only keeps proxies that have actual unlock data.
                    bool hasUsefulData =
                        proxy.toUnlock != StoryEvent.None ||
                        !string.IsNullOrEmpty(proxy.uniqueID);

                    if (!hasUsefulData)
                        continue;

                    int id = proxy.gameObject.GetInstanceID();
                    if (!seen.Add(id))
                        continue;

                    foundUpgrades.Add(proxy);

                    // Optional debug print with unlock info and position.
                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = proxy.transform.position;
                        DebugLogger.Log(
                            "Upgrade -> " +
                            proxy.gameObject.name + " -> " +
                            proxy.toUnlock + " -> " +
                            proxy.uniqueID + " -> (" +
                            pos.x + "," + pos.y + "," + pos.z + ")");
                    }
                }

                MelonLogger.Msg("[ShopUpgradeScanner] Found ShopUpgrades: " + foundUpgrades.Count);
                ShopUpgradeLogger.LogShopUpgrades(foundUpgrades);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ShopUpgradeScanner] Scan failed: " + ex.Message);
            }
        }
    }
}
// SoulScanner.cs
// Scans the room for soul objects by looking for roots with "salt ghost" in the hierarchy.

using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class SoulScanner
    {
        public static void Scan()
        {
            try
            {
                // Finds all MonoBehaviours in the room, then walks upward looking for soul roots.
                MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
                List<GameObject> foundSouls = new List<GameObject>();
                HashSet<int> seenRoots = new HashSet<int>();

                for (int i = 0; i < all.Length; i++)
                {
                    MonoBehaviour obj = all[i];
                    if (obj == null)
                        continue;

                    GameObject soulRoot = FindSoulRoot(obj.gameObject);
                    if (soulRoot == null)
                        continue;

                    int rootId = soulRoot.GetInstanceID();
                    if (!seenRoots.Add(rootId))
                        continue;

                    foundSouls.Add(soulRoot);

                    // Optional debug print with guessed soul info.
                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = soulRoot.transform.position;
                        DebugLogger.Log(
                            "Soul -> " +
                            GuessSoulName(soulRoot) + " -> " +
                            GuessSoulReward(soulRoot) + " -> (" +
                            pos.x + "," + pos.y + "," + pos.z + ")");
                    }
                }

                MelonLogger.Msg("[SoulScanner] Found Souls: " + foundSouls.Count);
                SoulLogger.LogSouls(foundSouls);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[SoulScanner] Scan failed: " + ex.Message);
            }
        }

        // Walks up the transform tree looking for a soul root object.
        private static GameObject FindSoulRoot(GameObject start)
        {
            if (start == null)
                return null;

            Transform current = start.transform;

            while (current != null)
            {
                string name = current.name != null ? current.name.ToLower() : "";

                if (name.Contains("salt ghost"))
                    return current.gameObject;

                current = current.parent;
            }

            return null;
        }

        // Best guess for the soul object's display name.
        public static string GuessSoulName(GameObject soulObj)
        {
            if (soulObj == null)
                return "UNKNOWN_SOUL";

            return soulObj.name ?? "UNKNOWN_SOUL";
        }

        // Best guess for the soul reward name.
        public static string GuessSoulReward(GameObject soulObj)
        {
            if (soulObj == null)
                return "UNKNOWN_REWARD";

            return soulObj.name ?? "UNKNOWN_REWARD";
        }

        // Returns true only if the rotation is big enough to be worth printing.
        public static bool ShouldShowRotation(Vector3 rot)
        {
            return Mathf.Abs(rot.x) > 1f ||
                   Mathf.Abs(rot.y) > 1f ||
                   Mathf.Abs(rot.z) > 1f;
        }
    }
}
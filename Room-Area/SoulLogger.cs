// SoulLogger.cs
// Saves soul scan results for the current room.

using System.Collections.Generic;
using UnityEngine;
using ArchDandara.Database;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class SoulLogger
    {
        // Saves the list of found soul root objects.
        public static void LogSouls(List<GameObject> list)
        {
            if (list == null || list.Count == 0)
                return;

            string scene = GetCurrentScene();

            DataManager.SaveRoomSouls(scene, list);

            // Optional debug print for each soul object.
            for (int i = 0; i < list.Count; i++)
            {
                GameObject soul = list[i];
                if (soul == null) continue;

                DebugLogger.Log("Soul logged: " + soul.name + " in " + scene);
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
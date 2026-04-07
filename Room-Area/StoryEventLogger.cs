// StoryEventLogger.cs
// Saves story-event-related objects found in the current room.

using System.Collections.Generic;
using UnityEngine;
using ArchDandara.Database;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class StoryEventLogger
    {
        // Saves the list of found story-event objects.
        public static void LogStoryEvents(List<GameObject> list)
        {
            if (list == null || list.Count == 0)
                return;

            string scene = GetCurrentScene();

            DataManager.SaveRoomStoryEvents(scene, list);

            // Optional debug print for each event object.
            for (int i = 0; i < list.Count; i++)
            {
                GameObject evt = list[i];
                if (evt == null) continue;

                DebugLogger.Log("StoryEvent logged: " + evt.name + " in " + scene);
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
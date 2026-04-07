// StoryEventScanner.cs
// Scans the current room for objects related to story/event triggers.

using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class StoryEventScanner
    {
        public static void Scan()
        {
            try
            {
                // Finds all MonoBehaviours, then filters down to known story-event types.
                MonoBehaviour[] all = Object.FindObjectsOfType<MonoBehaviour>();
                List<GameObject> foundEvents = new List<GameObject>();
                HashSet<int> seen = new HashSet<int>();

                for (int i = 0; i < all.Length; i++)
                {
                    MonoBehaviour obj = all[i];
                    if (obj == null)
                        continue;

                    bool isStoryEvent =
                        obj is StoryEventDependency ||
                        obj is StoryEventMultipleDependency ||
                        obj is StoryEventConditional ||
                        obj is DoEvent ||
                        obj is TimerEvent;

                    if (!isStoryEvent)
                        continue;

                    GameObject go = obj.gameObject;
                    if (go == null)
                        continue;

                    int id = go.GetInstanceID();
                    if (!seen.Add(id))
                        continue;

                    foundEvents.Add(go);

                    // Optional debug print with position.
                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = go.transform.position;

                        DebugLogger.Log(
                            "Event -> " +
                            go.name + " -> (" +
                            pos.x + "," + pos.y + "," + pos.z + ")");
                    }
                }

                MelonLogger.Msg("[StoryEventScanner] Found StoryEvents: " + foundEvents.Count);
                StoryEventLogger.LogStoryEvents(foundEvents);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[StoryEventScanner] Scan failed: " + ex.Message);
            }
        }
    }
}
// NPCLogger.cs
// Saves NPC scan results for the current room.

using System.Collections.Generic;
using ArchDandara.Database;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class NPCLogger
    {
        // Saves the list of found NPC interactables.
        public static void LogNPCs(List<DialogueInteractable> list)
        {
            if (list == null || list.Count == 0)
                return;

            string scene = GetCurrentScene();

            DataManager.SaveRoomNPCs(scene, list);

            // Optional debug print for each NPC found.
            for (int i = 0; i < list.Count; i++)
            {
                DialogueInteractable npc = list[i];
                if (npc == null) continue;

                DebugLogger.Log("NPC logged: " + npc.name + " in " + scene);
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
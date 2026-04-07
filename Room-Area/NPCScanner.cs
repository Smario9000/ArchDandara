// NPCScanner.cs
// Scans the current room for DialogueInteractable objects and passes them to NPCLogger.

using System.Collections.Generic;
using MelonLoader;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class NPCScanner
    {
        public static void Scan()
        {
            try
            {
                // Finds all dialogue-based interactables in the room.
                DialogueInteractable[] interactables = Object.FindObjectsOfType<DialogueInteractable>();
                List<DialogueInteractable> foundNPCs = new List<DialogueInteractable>();

                for (int i = 0; i < interactables.Length; i++)
                {
                    DialogueInteractable npc = interactables[i];
                    if (npc == null)
                        continue;

                    foundNPCs.Add(npc);

                    // Optional debug print with position and type.
                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = npc.transform.position;

                        DebugLogger.Log(
                            "NPC -> " +
                            npc.name + " -> " +
                            npc.GetType().Name + " -> (" +
                            pos.x + "," + pos.y + "," + pos.z + ")");
                    }
                }

                MelonLogger.Msg("[NPCScanner] Found NPCs: " + foundNPCs.Count);
                NPCLogger.LogNPCs(foundNPCs);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NPCScanner] Scan failed: " + ex.Message);
            }
        }
    }
}
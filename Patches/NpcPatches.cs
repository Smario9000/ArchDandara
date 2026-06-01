/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for NPC interactions and dialogue-based checks.
 * Why: NPC checks need generic handling like chests while allowing dialogue to continue normally.
 * Notes: NPC interactions are allowed to continue so dialogue and story flags still play normally after AP check sending.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(DialogueInteractable), "Interact")]
    public static class NpcInteractPatch
    {
        private static bool Prefix(DialogueInteractable __instance)
        {
            if (__instance == null)
                return true;

            if (InteractionGateService.ShouldBlockNpc(GameAccess.CurrentScene))
                return false;

            SpecialNpcChecks.TrySendCreationStoneInteraction(__instance, "DialogueInteractable.Interact");

            string objectName = __instance.name != null ? __instance.name : "UNKNOWN_NPC";
            string roomName = GameAccess.CurrentRoomName;
            long locationId;
            if (SpecialNpcChecks.TryGetLocation(__instance, out locationId))
            {
                bool sentSpecial = APLocationSender.TrySend(locationId, LocationKey.Build("NPC", roomName, objectName));
                MLLog.Msg("[Patch][NPC] Special interaction: " + GameAccess.CurrentScene + " | " +
                                roomName + " | " + objectName + " | location=" + locationId +
                                " | sent=" + sentSpecial);
                return true;
            }

            if (!RuntimeLocationResolver.TryResolveNpcInteraction(__instance, roomName, out locationId))
            {
                MLLog.Warning("[Patch][NPC] Unmapped, allowing vanilla interaction: " + roomName + " | " +
                                    objectName);
                return true;
            }

            bool sent = APLocationSender.TrySend(locationId, LocationKey.Build("NPC", roomName, objectName));
            MLLog.Msg("[Patch][NPC] Interaction: " + roomName + " | " + objectName + " | location=" +
                            locationId + " | sent=" + sent);
            return true;
        }
    }
}

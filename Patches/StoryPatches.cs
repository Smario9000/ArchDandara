/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for StoryEvent unlocks.
 * Why: Important checks and vanilla rewards are StoryEvents, so this filters AP locations from non-location story noise.
 * Notes: Story patches should log ignored events sparingly because the game unlocks many story flags during normal play.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(StoryManager), "UnlockEvent")]
    public static class StoryUnlockPatch
    {
        private static bool Prefix(StoryEvent eventID, ref bool __result)
        {
            if (GrantContext.IsArchipelagoGrant)
                return true;

            if (eventID == StoryEvent.Weapon_Missile && GameAccess.CurrentScene == "A1_GD14")
            {
                JonnyBMissileFallbackService.TrySendMissileAltarCheck(
                    "Story|A1_GD14|Weapon_Missile=>Missile Alter",
                    "Weapon_Missile story event");
                __result = false;
                MLLog.Msg("[Patch][Story] Blocked Jonny B. vanilla missile story reward: " + eventID);
                return false;
            }

            if (ItemIds.IsDlcTrialGateStoryEvent(eventID))
            {
                StoryLocationMap.TrySendStoryLocation(eventID, GameAccess.CurrentScene);

                if (InteractionGateService.ShouldBlockStoryEvent(eventID))
                {
                    __result = false;
                    MLLog.Msg("[Patch][Story] Blocked DLC trial story event until AP item is received: " +
                                    eventID);
                    return false;
                }

                return true;
            }

            if (!ItemIds.IsVanillaStoryRewardBlocked(eventID))
                return true;

            StoryLocationMap.TrySendStoryLocation(eventID, GameAccess.CurrentScene);

            __result = false;
            MLLog.Msg("[Patch][Story] Blocked vanilla AP story reward: " + eventID);
            return false;
        }

        private static void Postfix(StoryEvent eventID, bool __result)
        {
            if (GrantContext.IsArchipelagoGrant)
                return;

            if (!__result)
                return;

            if (StoryLocationMap.TrySendStoryLocation(eventID, GameAccess.CurrentScene))
                return;

            if (SpecialNpcChecks.TrySendCreationStoneStoryEvent(eventID))
                return;

            if (JonnyBMissileFallbackService.TrySendFromStoryEvent(eventID))
                return;

            MLLog.Msg("[Patch][Story] Ignored non-location story event: " + eventID);
        }
    }
}

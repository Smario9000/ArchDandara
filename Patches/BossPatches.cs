/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for boss defeat and boss StoryEvent checks.
 * Why: Boss kills are AP locations and goals, and received boss-key items should not skip the fight.
 * Notes: Boss patches report kills as checks but should not consume AP boss keys as automatic boss defeats.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(BossHealthContainer), "Die")]
    public static class BossHealthContainerDiePatch
    {
        private static void Postfix(BossHealthContainer __instance)
        {
            if (__instance == null || GrantContext.IsArchipelagoGrant)
                return;

            StoryEvent killEvent = __instance.killEvent;
            MLLog.Msg("[Patch][Boss] BossHealthContainer.Die killEvent=" + killEvent + " scene=" +
                            GameAccess.CurrentScene);

            if (ItemIds.IsVanillaStoryRewardBlocked(killEvent))
                return;

            StoryLocationMap.TrySendStoryLocation(killEvent, GameAccess.CurrentScene);
        }
    }

    [HarmonyPatch(typeof(AI_Boss_General), "FinishEverything")]
    public static class AIBossGeneralFinishEverythingPatch
    {
        private static void Postfix(AI_Boss_General __instance)
        {
            if (__instance == null || GrantContext.IsArchipelagoGrant)
                return;

            StoryEvent bossStoryEvent = __instance.bossStoryEvent;
            MLLog.Msg("[Patch][Boss] AI_Boss_General.FinishEverything bossStoryEvent=" + bossStoryEvent +
                            " scene=" + GameAccess.CurrentScene);

            FinalBossGoalFallback.TrySendFromFinishedBoss(bossStoryEvent, "AI_Boss_General.FinishEverything");

            if (ItemIds.IsVanillaStoryRewardBlocked(bossStoryEvent))
                return;

            StoryLocationMap.TrySendStoryLocation(bossStoryEvent, GameAccess.CurrentScene);
        }

    }

    internal static class FinalBossGoalFallback
    {
        public static void TrySendFromFinishedBoss(StoryEvent storyEvent, string source)
        {
            if (GameAccess.CurrentScene != "AF_Boss" || storyEvent != StoryEvent.Boss_Kill_6)
                return;

            MLLog.Msg("[Patch][Boss] AF_Boss Eldar final phase finished from " + source +
                            " | storyEvent=" + storyEvent);
            StoryLocationMap.TrySendStoryLocation(storyEvent, GameAccess.CurrentScene);

            if (ArchDandara.Archipelago.APSlotSettings.IsFinalBossGoal)
                ArchDandara.Archipelago.APClient.SendGoalAchieved(source + "|AF_Boss|Boss_Kill_6");
        }
    }
}

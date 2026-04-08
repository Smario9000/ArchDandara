// newgameseedintrostatelogic.cs
// Seeds important story flags on a fresh skipped game
// so the intro/tutorial state is treated as already completed.

using HarmonyLib;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(GameManager), "OnTransitionEnded")]
    public static class NewGameSeedIntroStateLogic
    {
        private static bool _seedAttemptedThisSession;

        private static void Postfix()
        {
            try
            {
                if (!SkipCutsceneConfig.Enabled)
                    return;

                if (!SkipCutsceneConfig.SeedIntroState)
                    return;

                if (_seedAttemptedThisSession)
                    return;

                var gm = PersistentSingleton<GameManager>.instance;
                if ((object)gm == null)
                    return;

                string scene = gm.GetCurrentScene();

                if (scene != SkipCutsceneConfig.StartCampScene)
                    return;

                if ((object)PersistentSingleton<StoryManager>.instance == null)
                {
                    MelonLogger.Warning("[NewGameSeedIntroStateLogic] StoryManager instance is null.");
                    return;
                }

                _seedAttemptedThisSession = true;

                StoryManager story = PersistentSingleton<StoryManager>.instance;

                UnlockIfNeeded(story, StoryEvent.Started);
                UnlockIfNeeded(story, StoryEvent.PU_Health);
                UnlockIfNeeded(story, StoryEvent.PU_Money);
                UnlockIfNeeded(story, StoryEvent.PU_DandaraArrow);
                UnlockIfNeeded(story, StoryEvent.PU_Money_Fear);
                UnlockIfNeeded(story, StoryEvent.HUD_Unlock);

                MelonLogger.Msg("[NewGameSeedIntroStateLogic] SUCCESS: seeded intro-complete story state.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[NewGameSeedIntroStateLogic] Exception while seeding intro story state: " + ex);
            }
        }

        private static void UnlockIfNeeded(StoryManager story, StoryEvent eventId)
        {
            if (story.GetEvent(eventId))
            {
                if (SkipCutsceneConfig.VerboseDebug)
                    MelonLogger.Msg("[NewGameSeedIntroStateLogic] Already set: " + eventId);

                return;
            }

            bool result = story.UnlockEvent(eventId);

            if (SkipCutsceneConfig.VerboseDebug)
            {
                MelonLogger.Msg(
                    "[NewGameSeedIntroStateLogic] Set " + eventId +
                    " | Result=" + result);
            }
        }
    }
}
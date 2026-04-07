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
        // Prevents seeding more than once per session.
        private static bool _seedAttemptedThisSession;

        private static void Postfix()
        {
            try
            {
                if (_seedAttemptedThisSession)
                    return;

                var gm = PersistentSingleton<GameManager>.instance;
                if ((object)gm == null)
                    return;

                string scene = gm.GetCurrentScene();

                // Only seed after landing in the new starting room.
                if (scene != "A1_ForestEdge")
                    return;

                bool hasExistingSave = false;
                if ((object)PersistentSingleton<SaveManager>.instance != null)
                {
                    hasExistingSave = PersistentSingleton<SaveManager>.instance.Has("StoryManager.StoryState");
                }

                MelonLogger.Msg(
                    "[NewGameSeedIntroStateLogic] OnTransitionEnded in " + scene +
                    " | HasStorySave=" + hasExistingSave);

                if ((object)PersistentSingleton<StoryManager>.instance == null)
                {
                    MelonLogger.Warning("[NewGameSeedIntroStateLogic] StoryManager instance is null.");
                    return;
                }

                _seedAttemptedThisSession = true;

                StoryManager story = PersistentSingleton<StoryManager>.instance;

                // Marks core intro/tutorial events as completed.
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

        // Unlocks a story event only if it is not already set.
        private static void UnlockIfNeeded(StoryManager story, StoryEvent eventId)
        {
            if (story.GetEvent(eventId))
            {
                MelonLogger.Msg("[NewGameSeedIntroStateLogic] Already set: " + eventId);
                return;
            }

            bool result = story.UnlockEvent(eventId);

            MelonLogger.Msg(
                "[NewGameSeedIntroStateLogic] Set " + eventId +
                " | Result=" + result);
        }
    }
}
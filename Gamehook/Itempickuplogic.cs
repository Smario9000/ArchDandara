// Itempickuplogic.cs
// Logs when the game unlocks a powerup by any of several PowerupManager methods.

using System;
using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(PowerupManager), "TryUnlock", new Type[]
    {
        typeof(Powerup),
        typeof(string),
        typeof(UnityEngine.Transform),
        typeof(PowerupManager.DelPowerupUnlockFinished)
    })]
    public static class Patch_TryUnlock
    {
        // Logs the normal TryUnlock path.
        private static void Postfix(Powerup powerup, string uniquePowerupID)
        {
            Itempickuplogic.LogPowerup("TryUnlock", powerup, uniquePowerupID);
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "TryUnlockWithoutShow", new Type[] { typeof(Powerup) })]
    public static class Patch_TryUnlockWithoutShow_NoId
    {
        // Logs unlocks that do not include a unique ID.
        private static void Postfix(Powerup powerup)
        {
            Itempickuplogic.LogPowerup("TryUnlockWithoutShow", powerup, "");
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "TryUnlockWithoutShow", new Type[] { typeof(Powerup), typeof(string) })]
    public static class Patch_TryUnlockWithoutShow_WithId
    {
        // Logs unlocks that do include a unique ID.
        private static void Postfix(Powerup powerup, string uniquePowerupID)
        {
            Itempickuplogic.LogPowerup("TryUnlockWithoutShow", powerup, uniquePowerupID);
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "TryUnlockCustomShow")]
    public static class Patch_TryUnlockCustomShow
    {
        // Logs unlocks that use the custom-show method.
        private static void Postfix(Powerup powerup, string uniquePowerupID)
        {
            Itempickuplogic.LogPowerup("TryUnlockCustomShow", powerup, uniquePowerupID);
        }
    }

    public static class Itempickuplogic
    {
        // Shared helper used by all the unlock patches above.
        public static void LogPowerup(string sourceMethod, Powerup powerup, string uniqueID)
        {
            if (powerup == null)
                return;

            string scene = GetCurrentScene();
            string rewardName = GetRewardName(powerup);
            string eventName = GetStoryEventName(powerup);

            DataManager.LogCheck(
                "PowerupUnlock",
                scene,
                sourceMethod,
                rewardName,
                "UniqueID=" + uniqueID + " StoryEvent=" + eventName);

            MelonLogger.Msg(
                "[LOG][Powerup] " +
                scene + " -> " +
                rewardName + " | " +
                sourceMethod + " | " +
                uniqueID + " | " +
                eventName);
        }

        // Tries to get the nicest readable reward name.
        private static string GetRewardName(Powerup powerup)
        {
            try
            {
                if (powerup.character != null && !string.IsNullOrEmpty(powerup.character.characterName))
                    return powerup.character.characterName;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[Itempickuplogic] Failed to read powerup.character.characterName: " + ex.Message);
            }

            return powerup != null && !string.IsNullOrEmpty(powerup.name)
                ? powerup.name
                : "UNKNOWN_POWERUP";
        }

        // Tries to get the story event linked to this powerup.
        private static string GetStoryEventName(Powerup powerup)
        {
            try
            {
                if (powerup.character != null)
                    return powerup.character.storyEvent.ToString();
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[Itempickuplogic] Failed to read powerup.character.storyEvent: " + ex.Message);
            }

            return "NONE";
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
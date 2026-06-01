/*
 * ArchDandara documentation
 * Purpose: Blocks vanilla powerup unlocks unless the grant context allows them.
 * Why: Randomizer checks should send AP locations, not give vanilla rewards, except during AP item application.
 * Notes: This is the main vanilla reward firewall; any exception here should be tied to GrantContext or a deliberate interaction state.
 */

using System;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PowerupManager), "TryUnlock", new Type[]
    {
        typeof(Powerup),
        typeof(string),
        typeof(UnityEngine.Transform),
        typeof(PowerupManager.DelPowerupUnlockFinished)
    })]
    public static class PowerupTryUnlockPatch
    {
        private static bool Prefix(Powerup powerup, string uniquePowerupID)
        {
            return PowerupPatchLogic.ShouldAllowPowerupUnlock("TryUnlock", powerup, uniquePowerupID);
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "TryUnlockWithoutShow", new Type[] { typeof(Powerup) })]
    public static class PowerupTryUnlockWithoutShowPatch
    {
        private static bool Prefix(Powerup powerup)
        {
            return PowerupPatchLogic.ShouldAllowPowerupUnlock("TryUnlockWithoutShow", powerup, "");
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "TryUnlockWithoutShow", new Type[] { typeof(Powerup), typeof(string) })]
    public static class PowerupTryUnlockWithoutShowWithIdPatch
    {
        private static bool Prefix(Powerup powerup, string uniquePowerupID)
        {
            return PowerupPatchLogic.ShouldAllowPowerupUnlock("TryUnlockWithoutShow", powerup, uniquePowerupID);
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "TryUnlockCustomShow")]
    public static class PowerupTryUnlockCustomShowPatch
    {
        private static bool Prefix(Powerup powerup, string uniquePowerupID)
        {
            return PowerupPatchLogic.ShouldAllowPowerupUnlock("TryUnlockCustomShow", powerup, uniquePowerupID);
        }
    }

    public static class PowerupPatchLogic
    {
        public static bool ShouldAllowPowerupUnlock(string methodName, Powerup powerup, string uniqueId)
        {
            // AP grants deliberately pass through the same vanilla unlock code to trigger side
            // effects. Everything else is blocked so opening a randomized chest cannot also give
            // the vanilla item.
            if (GrantContext.IsArchipelagoGrant || GrantContext.IsVanillaPowerupInteraction)
                return true;

            string rewardName = GetRewardName(powerup);
            MLLog.Msg("[Patch][Powerup] Blocked vanilla unlock: " + methodName + " | " + rewardName + " | " +
                            uniqueId);

            return false;
        }

        private static string GetRewardName(Powerup powerup)
        {
            if (powerup == null)
                return "UNKNOWN_POWERUP";

            try
            {
                if (powerup.character != null && !string.IsNullOrEmpty(powerup.character.characterName))
                    return powerup.character.characterName;
            }
            catch
            {
            }

            return !string.IsNullOrEmpty(powerup.name) ? powerup.name : "UNKNOWN_POWERUP";
        }
    }
}

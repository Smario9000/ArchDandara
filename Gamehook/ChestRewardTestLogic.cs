// ChestRewardTestLogic.cs
// Temporarily swaps one chest's reward so you can test what rewards still work from chest flow.

using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(PowerupInteractable), "Interact")]
    public static class ChestRewardTestLogic
    {
        // Turn this on only when actively testing.
        private const bool Enabled = false;

        // Set these for the chest you want to test.
        private const string TargetScene = "A1_PainterPath1";
        private const string TargetChestName = "ChestMap";
        private const StoryEvent TestReward = StoryEvent.PU_HealthFlask;

        // Reflection access to the chest's private reward field.
        private static readonly FieldInfo PowerupField =
            AccessTools.Field(typeof(PowerupInteractable), "_powerup");

        private static void Prefix(PowerupInteractable __instance)
        {
            try
            {
                if (!Enabled)
                    if (__instance == null) return;

                string scene = GetCurrentScene();
                if (scene != TargetScene)
                    return;

                string chestName = __instance.name ?? string.Empty;
                if (chestName != TargetChestName)
                    return;

                if ((object)PowerupField == null)
                {
                    MelonLogger.Error("[ChestRewardTestLogic] Could not find PowerupInteractable._powerup");
                    return;
                }

                PowerupManager manager = PersistentSingleton<PowerupManager>.instance;
                if (manager == null)
                {
                    MelonLogger.Error("[ChestRewardTestLogic] PowerupManager instance was null");
                    return;
                }

                Powerup replacement = manager.GetPowerup(TestReward);
                if (replacement == null)
                {
                    MelonLogger.Error("[ChestRewardTestLogic] GetPowerup returned null for " + TestReward);
                    return;
                }

                Powerup oldPowerup = GetChestPowerup(__instance);
                string oldName = GetRewardName(oldPowerup);
                string newName = GetRewardName(replacement);

                PowerupField.SetValue(__instance, replacement);

                MelonLogger.Msg(
                    "[ChestRewardTestLogic] Swapped chest reward in " + scene +
                    " | Chest=" + chestName +
                    " | Old=" + oldName +
                    " | New=" + newName +
                    " | StoryEvent=" + TestReward);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ChestRewardTestLogic] Exception: " + ex);
            }
        }

        private static Powerup GetChestPowerup(PowerupInteractable chest)
        {
            try
            {
                if ((object)PowerupField == null || chest == null)
                    return null;

                return (Powerup)PowerupField.GetValue(chest);
            }
            catch
            {
                return null;
            }
        }

        private static string GetRewardName(Powerup powerup)
        {
            if (powerup == null)
                return "NULL_POWERUP";

            try
            {
                if (powerup.character != null && !string.IsNullOrEmpty(powerup.character.characterName))
                    return powerup.character.characterName;
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[ChestRewardTestLogic] Exception: " + ex);
            }

            return !string.IsNullOrEmpty(powerup.name) ? powerup.name : "UNKNOWN_POWERUP";
        }

        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
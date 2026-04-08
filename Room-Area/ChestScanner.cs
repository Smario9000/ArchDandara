using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Room_Area
{
    public static class ChestScanner
    {
        private static readonly FieldInfo PowerupField =
            AccessTools.Field(typeof(PowerupInteractable), "_powerup");

        public static void Scan()
        {
            try
            {
                PowerupInteractable[] chests = Object.FindObjectsOfType<PowerupInteractable>();
                List<PowerupInteractable> foundChests = new List<PowerupInteractable>();

                for (int i = 0; i < chests.Length; i++)
                {
                    PowerupInteractable chest = chests[i];
                    if (chest == null)
                        continue;

                    foundChests.Add(chest);

                    if (DebugLogger.Enabled)
                    {
                        Vector3 pos = chest.transform.position;

                        DebugLogger.Log(
                            "Chest -> " +
                            chest.name + " -> " +
                            chest.GetType().Name + " -> " +
                            "Reward=" + GetRewardName(chest) + " -> " +
                            "StoryEvent=" + GetStoryEventName(chest) + " -> " +
                            pos.x + "," + pos.y + "," + pos.z + ")");
                    }
                }

                MelonLogger.Msg("[ChestScanner] Found chests: " + foundChests.Count);
                ChestLogger.LogChests(foundChests);
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error("[ChestScanner] Scan failed: " + ex.Message);
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

        private static string GetRewardName(PowerupInteractable chest)
        {
            try
            {
                Powerup powerup = GetChestPowerup(chest);
                if (powerup == null)
                    return "NULL_POWERUP";

                if (powerup.character != null && !string.IsNullOrEmpty(powerup.character.characterName))
                    return powerup.character.characterName;

                return !string.IsNullOrEmpty(powerup.name) ? powerup.name : "UNKNOWN_POWERUP";
            }
            catch
            {
                return "ERROR_REWARD";
            }
        }

        private static string GetStoryEventName(PowerupInteractable chest)
        {
            try
            {
                Powerup powerup = GetChestPowerup(chest);
                if (powerup == null)
                    return "NONE";

                if (powerup.character != null)
                    return powerup.character.storyEvent.ToString();

                return "NONE";
            }
            catch
            {
                return "ERROR_EVENT";
            }
        }
    }
}
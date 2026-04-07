// Shoppurchaselogic.cs
// Logs successful shop purchases and current shop progression values.

using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(PowerupManager), "PurchasePowerup")]
    public static class Patch_PurchasePowerup
    {
        // Runs after a shop purchase is attempted.
        private static void Postfix(Powerup powerup, bool __result)
        {
            // Only log successful purchases.
            if (!__result || powerup == null)
                return;

            string scene = GetCurrentScene();
            string rewardName = GetRewardName(powerup);

            // Reads current shop state from the game's powerup manager.
            PowerupManager manager = PersistentSingleton<PowerupManager>.instance;

            int currentLevel = -1;
            int maxLevel = -1;
            int nextPrice = -1;
            bool canAffordNext = false;
            int purchasedLevel = -1;
            int remainingLevels = -1;

            if (manager != null)
            {
                currentLevel = manager.GetCurrentPlayerLevel();
                maxLevel = manager.GetCurrentPlayerMaxLevel();
                nextPrice = manager.PowerupPrice();
                canAffordNext = manager.HasMoneyForNextPowerup();

                purchasedLevel = currentLevel;
                remainingLevels = maxLevel >= 0 && currentLevel >= 0
                    ? (maxLevel - currentLevel)
                    : -1;
            }

            // Packs shop progress info into one log string.
            string extra =
                "PurchasedLevel=" + purchasedLevel +
                " CurrentLevel=" + currentLevel +
                " MaxLevel=" + maxLevel +
                " RemainingLevels=" + remainingLevels +
                " NextPrice=" + nextPrice +
                " CanAffordNext=" + canAffordNext;

            DataManager.LogCheck(
                "ShopPurchase",
                scene,
                "PurchasePowerup",
                rewardName,
                extra);

            DataManager.LogActivity(
                "ShopPurchase",
                scene,
                rewardName,
                extra);

            MelonLogger.Msg(
                "[LOG][Shop] " +
                scene + " -> " +
                rewardName + " | " +
                extra);
        }

        // Tries to get a clean display name for the purchased powerup.
        private static string GetRewardName(Powerup powerup)
        {
            try
            {
                if (powerup.character != null && !string.IsNullOrEmpty(powerup.character.characterName))
                    return powerup.character.characterName;
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[Shoppurchaselogic] Failed to read powerup.character.characterName: " + ex.Message);
            }

            return powerup != null && !string.IsNullOrEmpty(powerup.name)
                ? powerup.name
                : "UNKNOWN_POWERUP";
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }

    // Empty holder class for organization / future logic.
    public static class Shoppurchaselogic
    {
    }
}
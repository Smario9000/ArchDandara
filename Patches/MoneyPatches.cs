/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for money and salt gain/loss behavior.
 * Why: Salt scaling, death recovery, AP salt items, and menu warps need different rules from vanilla money flow.
 * Notes: Money patches must distinguish AP grants, normal drops, death recovery, and menu travel.
 */

using HarmonyLib;
using ArchDandara.Archipelago;
using ArchDandara.Game;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PlayerController), "AddMoney")]
    public static class MoneyPatch
    {
        private static void Prefix(ref int money)
        {
            if (GrantContext.IsArchipelagoGrant || money <= 0)
                return;

            int scaled = (int)(money * APSlotSettings.SaltDropMultiplier + 0.5f);
            money = scaled < 0 ? 0 : scaled;
        }
    }

    [HarmonyPatch(typeof(MoneyDrop), "GetMoney")]
    public static class MoneyDropGetMoneyPatch
    {
        private static bool Prefix(MoneyDrop __instance)
        {
            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(__instance, null) || object.ReferenceEquals(player, null))
                return true;

            PlayerController.PlayerState.PlayerDeathLocation deathLocation = player.GetPlayerDeathPlace();
            if (!IsDeathRecoveryToken(__instance, player, deathLocation))
                return true;

            int recovered = APSlotSettings.ScaleDeathRecoveryMoney(__instance.Amount);
            GrantContext.IsArchipelagoGrant = true;
            try
            {
                if (recovered > 0)
                    player.AddMoney(recovered);

                player.DeleteCurrentStateForDeath();
            }
            finally
            {
                GrantContext.IsArchipelagoGrant = false;
            }

            MLLog.Msg("[Patch][Money] Recovered death salt without drop multiplier: original=" +
                            __instance.Amount + " recovered=" + recovered);
            return false;
        }

        private static bool IsDeathRecoveryToken(MoneyDrop moneyDrop, PlayerController player,
            PlayerController.PlayerState.PlayerDeathLocation deathLocation)
        {
            if (object.ReferenceEquals(deathLocation, null))
                return false;

            if (moneyDrop.Amount != deathLocation.amountMoney)
                return false;

            if (GameAccess.CurrentScene != deathLocation.sceneName)
                return false;

            if (!object.ReferenceEquals(player.moneyRetriavalTokenPrefab, null) &&
                !string.IsNullOrEmpty(player.moneyRetriavalTokenPrefab.name) &&
                !string.IsNullOrEmpty(moneyDrop.name) &&
                moneyDrop.name.IndexOf(player.moneyRetriavalTokenPrefab.name) >= 0)
                return true;

            float dx = moneyDrop.transform.position.x - deathLocation.positionX;
            float dy = moneyDrop.transform.position.y - deathLocation.positionY;
            return dx * dx + dy * dy < 9.0f;
        }
    }
}

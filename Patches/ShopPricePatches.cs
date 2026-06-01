/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for shop level, max level, price, and CanUnlock behavior.
 * Why: Prices and availability should reflect AP bought-check state instead of vanilla powerup counts.
 * Notes: Patch exact overloads here to avoid Harmony ambiguous-match errors on old Mono reflection.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    internal static class ShopPricePatchService
    {
        public static int CurrentApShopLevel()
        {
            return ShopLocationResolver.GetGlobalCheckedCount();
        }

        public static int CurrentApShopPrice()
        {
            return ShopPriceService.PriceForLevel(CurrentApShopLevel());
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "GetCurrentPlayerLevel")]
    public static class PowerupManagerGetCurrentPlayerLevelPatch
    {
        private static void Postfix(ref int __result)
        {
            __result = ShopPricePatchService.CurrentApShopLevel();
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "GetCurrentPlayerMaxLevel")]
    public static class PowerupManagerGetCurrentPlayerMaxLevelPatch
    {
        private static void Postfix(ref int __result)
        {
            __result = ShopPriceService.MaxShopChecks;
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "PowerupPriceForLevel")]
    public static class PowerupManagerPowerupPriceForLevelPatch
    {
        private static void Postfix(int lvl, ref int __result)
        {
            __result = ShopPricePatchService.CurrentApShopPrice();
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "PowerupPrice")]
    public static class PowerupManagerPowerupPricePatch
    {
        private static void Postfix(ref int __result)
        {
            __result = ShopPricePatchService.CurrentApShopPrice();
        }
    }

    [HarmonyPatch(typeof(PowerupManager), "HasMoneyForNextPowerup")]
    public static class PowerupManagerHasMoneyForNextPowerupPatch
    {
        private static void Postfix(ref bool __result)
        {
            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(player, null))
                return;

            __result = ShopPricePatchService.CurrentApShopPrice() <= player.GetCurrentMoney();
        }
    }

    [HarmonyPatch(typeof(Powerup), "CanUnlock", new System.Type[0])]
    public static class PowerupCanUnlockPatch
    {
        private static void Postfix(Powerup __instance, ref bool __result)
        {
            if (GrantContext.IsArchipelagoGrant)
                return;

            ShopCategory category;
            if (!ShopLocationResolver.TryGetCategory(__instance, out category))
                return;

            __result = ShopLocationResolver.GetCheckedCount(category) < category.MaxBuyCount;
        }
    }
}

/*
 * ArchDandara documentation
 * Purpose: Calculates AP-controlled shop upgrade counts and prices.
 * Why: Shop costs and availability should depend on bought checks, not on AP item receipts.
 * Notes: Prices use bought-check count so received AP upgrade items do not make future shop checks more expensive.
 */

using ArchDandara.Archipelago;

namespace ArchDandara.Game
{
    public static class ShopPriceService
    {
        private static readonly int[] BasePrices =
        {
            // These are the vanilla-style cumulative shop prices we use as the AP baseline.
            // Slot settings scale the final value, but the index always comes from bought AP checks.
            420, 675, 955, 1200, 1350, 1505, 1665, 1830, 2002, 2179, 2362,
            2551, 2745, 2946, 3154, 3367, 3587, 3813, 4045, 4284, 4529,
            4781, 5039, 5304, 5575, 5853, 6138, 6429, 6729, 7033, 7344,
            7663, 7988, 8320, 8660, 9006, 9359, 9718, 10022, 10358,
            10693, 11029, 11365, 11701
        };

        public static int MaxShopChecks
        {
            get { return BasePrices.Length; }
        }

        public static int PriceForLevel(int level)
        {
            if (level < 0)
                level = 0;

            // After the authored price table ends, keep using the final price rather than falling
            // back to the game's formula. AP shop counts are finite and should stay predictable.
            int basePrice = level < BasePrices.Length ? BasePrices[level] : BasePrices[BasePrices.Length - 1];
            return ScalePrice(basePrice, APSlotSettings.ShopCostMultiplier);
        }

        public static int ScalePrice(int price, float multiplier)
        {
            if (price <= 0)
                return 0;

            int result = (int)(price * multiplier + 0.5f);
            // A heavily discounted setting should still cost at least one salt so purchases remain
            // observable and cannot become free by rounding.
            return result < 1 ? 1 : result;
        }
    }
}

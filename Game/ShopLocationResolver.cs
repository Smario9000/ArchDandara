/*
 * ArchDandara documentation
 * Purpose: Maps shop purchase categories and levels to AP shop-check location ids.
 * Why: Each purchase is an AP check, and this keeps category and level naming aligned with the APWorld.
 * Notes: Shop category names must match SaveSync category keys and APWorld location names.
 */

using ArchDandara.Archipelago;

namespace ArchDandara.Game
{
    public static class ShopLocationResolver
    {
        public static bool TryGetCategory(Powerup powerup, out ShopCategory category)
        {
            category = null;

            if (object.ReferenceEquals(powerup, null) ||
                object.ReferenceEquals(powerup.character, null))
                return false;

            return TryGetCategory(powerup.character.storyEvent, out category);
        }

        public static bool TryGetCategory(StoryEvent storyEvent, out ShopCategory category)
        {
            category = null;

            switch (storyEvent)
            {
                case StoryEvent.PU_Health:
                    category = new ShopCategory("Heart", "Buy Heart Shop Upgrade #", "Heart Enhancer Permit",
                        "Heart of the Great Salt", 3, 20);
                    return true;
                case StoryEvent.PU_Ammo:
                    category = new ShopCategory("Mana", "Buy Mana Shop Upgrade #", "Freedom Enhancer Permit",
                        "Scarf of Freedom", 0, 10);
                    return true;
                case StoryEvent.PU_HealthFlaskUpgrade:
                    category = new ShopCategory("Health Flask", "Buy Health Flask Shop Upgrade #",
                        "Essence Enhancer Permit", "Essence of Salt Enhancer", 0, 7);
                    return true;
                case StoryEvent.PU_ManaFlaskUpgrade:
                    category = new ShopCategory("Mana Flask", "Buy Mana Flask Shop Upgrade #",
                        "Infusion Enhancer Permit", "Infusion of Salt Enhancer", 0, 7);
                    return true;
                default:
                    return false;
            }
        }

        public static int GetCheckedCount(ShopCategory category)
        {
            if (object.ReferenceEquals(category, null))
                return 0;

            return SaveSync.GetShopBoughtCount(category.DisplayName);
        }

        public static bool TryGetNextLocation(ShopCategory category, out long locationId, out string locationName)
        {
            locationId = 0;
            locationName = null;

            if (object.ReferenceEquals(category, null))
                return false;

            if (GetCheckedCount(category) >= category.MaxBuyCount)
                return false;

            int nextShopIndex = SaveSync.GetTotalShopBoughtCount() + 1;
            if (nextShopIndex < 1)
                nextShopIndex = 1;

            for (int i = nextShopIndex; i <= ShopPriceService.MaxShopChecks; i++)
            {
                string candidateName = "Buy Upgrade " + i;
                long candidateId;
                if (!LocationIds.TryGetLocationIdByName(candidateName, out candidateId))
                {
                    if (i == 1)
                        return false;

                    return false;
                }

                if (!SaveSync.HasCheckedLocation(candidateId))
                {
                    locationId = candidateId;
                    locationName = candidateName;
                    return true;
                }
            }

            for (int i = 1; i < nextShopIndex && i <= ShopPriceService.MaxShopChecks; i++)
            {
                string candidateName = "Buy Upgrade " + i;
                long candidateId;
                if (!LocationIds.TryGetLocationIdByName(candidateName, out candidateId))
                    return false;

                if (!SaveSync.HasCheckedLocation(candidateId))
                {
                    locationId = candidateId;
                    locationName = candidateName;
                    return true;
                }
            }

            return false;
        }

        public static int GetGlobalCheckedCount()
        {
            return SaveSync.GetTotalShopBoughtCount();
        }

        public static int GetCheckedShopLocationCount()
        {
            int count = 0;
            for (int i = 1; i <= ShopPriceService.MaxShopChecks; i++)
            {
                long locationId;
                if (!LocationIds.TryGetLocationIdByName("Buy Upgrade " + i, out locationId))
                    break;

                if (SaveSync.HasCheckedLocation(locationId))
                    count++;
            }

            return count;
        }
    }

    public class ShopCategory
    {
        public readonly string DisplayName;
        public readonly string LocationPrefix;
        public readonly string PermitItemName;
        public readonly string ReceivedItemName;
        public readonly int BaseFilledCount;
        public readonly int MaxBuyCount;

        public ShopCategory(string displayName, string locationPrefix, string permitItemName, string receivedItemName,
            int baseFilledCount, int maxBuyCount)
        {
            DisplayName = displayName;
            LocationPrefix = locationPrefix;
            PermitItemName = permitItemName;
            ReceivedItemName = receivedItemName;
            BaseFilledCount = baseFilledCount;
            MaxBuyCount = maxBuyCount;
        }
    }
}

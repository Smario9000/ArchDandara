/*
 * ArchDandara documentation
 * Purpose: Adjusts player salt after shop purchases and AP or new-game state restoration.
 * Why: This prevents reset exploits while preserving legitimate AP salt and bought-upgrade costs.
 * Notes: Only local shop purchases should subtract salt; AP money item scaling is handled elsewhere.
 */

namespace ArchDandara.Game
{
    public static class ShopSaltBalanceService
    {
        private static bool ApplyPending;
        private static float NextApplyTime;

        public static void RecordShopPurchase(int price)
        {
            if (price <= 0)
                return;

            SaveSync.AddShopSaltSpent(price);
            SaveSync.Save();
        }

        public static void RecalculateSpentFromCheckedShopLocations()
        {
            PowerupManager manager = GameAccess.PowerupManager;
            if (object.ReferenceEquals(manager, null))
            {
                ApplyPending = true;
                return;
            }

            int checkedShopLocations = ShopLocationResolver.GetCheckedShopLocationCount();
            int spent = 0;
            for (int i = 0; i < checkedShopLocations; i++)
                spent += ShopPriceService.PriceForLevel(i);

            SaveSync.SetShopSaltSpentMinimum(spent);
            SaveSync.Save();
            ApplyPending = true;
            MLLog.Msg("[ShopSalt] Recalculated spent salt from AP shop checks: " + spent +
                            " (" + checkedShopLocations + " bought)");
        }

        public static void ScheduleApply()
        {
            ApplyPending = true;
        }

        public static void Update()
        {
            if (!ApplyPending || UnityEngine.Time.time < NextApplyTime)
                return;

            if (ApplyCap())
            {
                ApplyPending = false;
                return;
            }

            NextApplyTime = UnityEngine.Time.time + 1.0f;
        }

        public static bool ApplyCap()
        {
            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(player, null))
                return false;

            int receivedSalt = CalculateReceivedApSalt();
            int spentSalt = SaveSync.GetShopSaltSpent();
            int allowedSalt = receivedSalt - spentSalt;
            if (allowedSalt < 0)
                allowedSalt = 0;

            int currentSalt = player.GetCurrentMoney();
            if (currentSalt > allowedSalt)
            {
                GrantContext.IsArchipelagoGrant = true;
                try
                {
                    player.SetMoney(allowedSalt);
                }
                finally
                {
                    GrantContext.IsArchipelagoGrant = false;
                }

                MLLog.Msg("[ShopSalt] Capped salt after AP shop spending: current=" + currentSalt +
                                " allowed=" + allowedSalt + " received=" + receivedSalt +
                                " spent=" + spentSalt);
            }

            return true;
        }

        private static int CalculateReceivedApSalt()
        {
            return GetReceivedSalt("Pleas of the Salt Fear") +
                   GetReceivedSalt("Pleas of the Salt") +
                   GetReceivedSalt("Salt") +
                   GetReceivedSalt("Salt 100") +
                   GetReceivedSalt("Salt 250") +
                   GetReceivedSalt("Salt 500") +
                   GetReceivedSalt("Salt 1000");
        }

        private static int GetReceivedSalt(string itemName)
        {
            int amount;
            if (!ItemIds.TryGetMoneyAmount(itemName, out amount))
                return 0;

            return SaveSync.GetReceivedItemCount(itemName) *
                   ArchDandara.Archipelago.APSlotSettings.ScaleApMoney(itemName, amount);
        }
    }
}

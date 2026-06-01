/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for shop purchases and permit gates.
 * Why: Shop upgrades are AP checks and require AP permit items.
 * Notes: Shop purchase patches must send AP checks once and then update local bought state for persistence.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PowerupManager), "PurchasePowerup")]
    public static class ShopPurchasePatch
    {
        private static string LastPurchaseCategory = "";
        private static float LastPurchaseTime;
        private const float PurchaseDebounceSeconds = 0.35f;

        private static bool Prefix(Powerup powerup)
        {
            if (GrantContext.IsArchipelagoGrant)
                return true;

            PowerupManager manager = GameAccess.PowerupManager;
            if (object.ReferenceEquals(manager, null))
                return true;

            ShopCategory category;
            if (!ShopLocationResolver.TryGetCategory(powerup, out category))
            {
                MLLog.Warning("[Patch][Shop] Unknown shop powerup, allowing vanilla purchase.");
                return true;
            }

            if (IsDuplicatePurchaseCall(category))
            {
                // The shop UI can invoke PurchasePowerup twice from one button press. Debouncing
                // here prevents one click from buying and sending two AP checks.
                MLLog.Msg("[Patch][Shop] Ignored duplicate " + category.DisplayName + " purchase call.");
                return false;
            }

            if (!SaveSync.HasShopPermit(category.PermitItemName))
            {
                MLLog.Msg("[Patch][Shop] Blocked " + category.DisplayName +
                                " purchase, missing AP permit: " + category.PermitItemName);
                return false;
            }

            int boughtCount = ShopLocationResolver.GetCheckedCount(category);
            if (boughtCount >= category.MaxBuyCount)
            {
                // Do not let the game continue spending salt after AP has no more locations for
                // this category. The visual refresh makes the completed state obvious in the shop.
                MLLog.Msg("[Patch][Shop] Blocked " + category.DisplayName +
                                " purchase, category is complete: " + boughtCount + "/" + category.MaxBuyCount);
                ShopBarVisualService.RefreshAll();
                return false;
            }

            long locationId;
            string locationName;
            if (!ShopLocationResolver.TryGetNextLocation(category, out locationId, out locationName))
            {
                MLLog.Msg("[Patch][Shop] No remaining AP shop checks for " + category.DisplayName);
                return false;
            }

            if (!manager.HasMoneyForNextPowerup())
            {
                MLLog.Msg("[Patch][Shop] Not enough salt for AP shop check: " + locationName);
                return false;
            }

            if (!APClient.Connected)
            {
                MLLog.Msg("[Patch][Shop] Not connected to AP, purchase not sent: " + locationName);
                return false;
            }

            PlayerController player = GameAccess.Player;
            int price = ShopPriceService.PriceForLevel(ShopLocationResolver.GetGlobalCheckedCount());
            if (object.ReferenceEquals(player, null) || !player.SpendMoney(price))
            {
                MLLog.Msg("[Patch][Shop] Could not spend salt for AP shop check: " + locationName);
                return false;
            }

            bool sent = APLocationSender.TrySend(locationId, locationName);
            if (sent)
            {
                // Only persist the local purchase after AP accepts the check. If sending fails,
                // the player should not lose the location or advance shop state.
                SaveSync.MarkShopBought(category.DisplayName);
                ShopSaltBalanceService.RecordShopPurchase(price);
                SaveSync.Save();
                ShopBarVisualService.RefreshAll();
            }

            MLLog.Msg("[Patch][Shop] AP shop purchase: " + category.DisplayName + " | " + locationName +
                            " | sent=" + sent);

            return false;
        }

        private static bool IsDuplicatePurchaseCall(ShopCategory category)
        {
            if (object.ReferenceEquals(category, null))
                return false;

            float now = UnityEngine.Time.time;
            if (LastPurchaseCategory == category.DisplayName && now - LastPurchaseTime < PurchaseDebounceSeconds)
                return true;

            LastPurchaseCategory = category.DisplayName;
            LastPurchaseTime = now;
            return false;
        }
    }
}

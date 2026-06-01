/*
 * ArchDandara documentation
 * Purpose: Updates shop upgrade bars to show bought, received, and combined AP/shop progress colors.
 * Why: Shop upgrades are split between local purchases and AP item receipts, so the UI needs to show both sources.
 * Notes: Bar colors represent AP state, not vanilla affordability; keep visual logic separate from purchase rules.
 */

using ArchDandara.Archipelago;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ArchDandara.Game
{
    public static class ShopBarVisualService
    {
        private static readonly Color EmptyColor = new Color(0.55f, 0.55f, 0.55f, 1.0f);
        private static bool LoggedPatchFailure;
        private static float NextRefreshLogTime;

        public static void Apply(PowerupShow show)
        {
            if (object.ReferenceEquals(show, null) || object.ReferenceEquals(show.powerupBar, null) ||
                object.ReferenceEquals(show.character, null))
                return;

            ShopCategory category;
            if (!ShopLocationResolver.TryGetCategory(show.character.storyEvent, out category))
                return;

            int boughtCount = ShopLocationResolver.GetCheckedCount(category);
            int receivedCount = SaveSync.GetReceivedItemCount(category.ReceivedItemName);
            if (boughtCount > category.MaxBuyCount)
                boughtCount = category.MaxBuyCount;

            // Bought count is what the player paid for locally; received count is how many upgrades
            // AP has sent back. The bar colors compare those two independent progress tracks.
            LogRefresh(category, boughtCount, receivedCount);
            Apply(show.powerupBar, boughtCount, receivedCount, category.BaseFilledCount, category.DisplayName);
        }

        public static void RefreshAll()
        {
            try
            {
                PowerupShow[] shows = Object.FindObjectsOfType<PowerupShow>();
                if (shows == null)
                    return;

                for (int i = 0; i < shows.Length; i++)
                    Apply(shows[i]);
            }
            catch (System.Exception ex)
            {
                if (!LoggedPatchFailure)
                {
                    LoggedPatchFailure = true;
                    MLLog.Warning("[ShopBar] Failed to refresh shop bars: " +
                                        ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        private static void Apply(PowerupBar bar, int boughtCount, int receivedCount, int baseFilledCount,
            string displayName)
        {
            try
            {
                Image[] slices = GetSlices(bar);
                if (slices == null)
                    return;

                int max = slices.Length;
                int visualBoughtCount = boughtCount;
                int visualReceivedCount = receivedCount + baseFilledCount;

                if (visualBoughtCount > max)
                    visualBoughtCount = max;
                if (visualReceivedCount > max)
                    visualReceivedCount = max;

                for (int i = 0; i < max; i++)
                {
                    Image slice = slices[i];
                    if (object.ReferenceEquals(slice, null))
                        continue;

                    // Older versions drew overlays. Remove them first so switching color modes or
                    // reconnecting cannot leave stale child images on the bar.
                    RemoveOverlay(slice, "ArchDandara_BoughtTop");
                    RemoveOverlay(slice, "ArchDandara_ReceivedBottom");

                    bool bought = i < visualBoughtCount;
                    bool received = i < visualReceivedCount;

                    if (bought && received)
                    {
                        // Combined color means this index was both purchased locally and received
                        // from AP, so the upgrade is fully represented on both sides.
                        slice.sprite = bar.spriteFilled;
                        slice.color = APSlotSettings.ShopReceivedColor;
                    }
                    else if (bought)
                    {
                        slice.sprite = bar.spriteFilled;
                        slice.color = APSlotSettings.ShopBoughtColor;
                    }
                    else if (received)
                    {
                        slice.sprite = bar.spriteFilled;
                        slice.color = APSlotSettings.ShopReceivedOnlyColor;
                    }
                    else
                    {
                        slice.sprite = bar.spriteEmpty;
                        slice.color = EmptyColor;
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (!LoggedPatchFailure)
                {
                    LoggedPatchFailure = true;
                    MLLog.Warning("[ShopBar] Failed to update shop bar " + displayName + ": " +
                                        ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        private static void LogRefresh(ShopCategory category, int boughtCount, int receivedCount)
        {
            if (object.ReferenceEquals(category, null) || Time.time < NextRefreshLogTime)
                return;

            NextRefreshLogTime = Time.time + 0.5f;
            MLLog.Msg("[ShopBar] " + category.DisplayName +
                            " bought=" + boughtCount +
                            " received=" + receivedCount +
                            " base=" + category.BaseFilledCount +
                            " permit=" + category.PermitItemName +
                            " receivedItem=" + category.ReceivedItemName);
        }

        private static void RemoveOverlay(Image parentSlice, string objectName)
        {
            Transform existing = parentSlice.transform.Find(objectName);
            if (!object.ReferenceEquals(existing, null))
                Object.Destroy(existing.gameObject);
        }

        private static Image[] GetSlices(PowerupBar bar)
        {
            System.Reflection.FieldInfo field = AccessTools.Field(typeof(PowerupBar), "slices");
            if (object.ReferenceEquals(field, null))
                return null;

            return field.GetValue(bar) as Image[];
        }
    }
}

/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for shop bar rendering and refresh points.
 * Why: The vanilla shop UI only knows bought levels, while AP needs bought, received, and combined visual states.
 * Notes: Shop bar patches should only affect visual state; purchase validation belongs in shop and price services.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PowerupShow), "ResetInfo")]
    public static class PowerupShowResetInfoPatch
    {
        private static void Postfix(PowerupShow __instance)
        {
            ShopBarVisualService.Apply(__instance);
        }
    }
}

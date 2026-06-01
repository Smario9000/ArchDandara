/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for respawn, return-to-flag, and post-death state restoration.
 * Why: AP changes must survive respawns and avoid salt loss when using modded menu travel.
 * Notes: Respawn hooks reapply AP state after the game rebuilds player state from its own save data.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PlayerController), "ForcePlayerDeath")]
    public static class ForcePlayerDeathPatch
    {
        private static void Prefix(PlayerController __instance, ref int __state)
        {
            __state = -1;
            if (object.ReferenceEquals(__instance, null))
                return;

            __state = __instance.GetCurrentMoney();
        }

        private static void Postfix(PlayerController __instance, int __state)
        {
            if (object.ReferenceEquals(__instance, null) || __state < 0)
                return;

            __instance.SetMoney(__state);
            DamageUpgradeService.ReapplyAfterPlayerReset();
            MLLog.Msg("[Patch][Respawn] Preserved salt after forced respawn: " + __state);
        }
    }

    [HarmonyPatch(typeof(PlayerController), "ReturnToHoistedFlag")]
    public static class ReturnToHoistedFlagPatch
    {
        private static void Prefix(PlayerController __instance, ref int __state)
        {
            __state = -1;
            if (object.ReferenceEquals(__instance, null))
                return;

            __state = __instance.GetCurrentMoney();
        }

        private static void Postfix(PlayerController __instance, int __state)
        {
            if (object.ReferenceEquals(__instance, null) || __state < 0)
                return;

            __instance.SetMoney(__state);
            DamageUpgradeService.ReapplyAfterPlayerReset();
            MLLog.Msg("[Patch][Respawn] Preserved salt after return to flag: " + __state);
        }
    }

    [HarmonyPatch(typeof(GameManager), "OnPlayerReturnToHoistedFlag")]
    public static class GameManagerReturnToHoistedFlagPatch
    {
        private static void Prefix(ref int __state)
        {
            __state = -1;
            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(player, null))
                return;

            __state = player.GetCurrentMoney();
        }

        private static void Postfix(int __state)
        {
            if (__state < 0)
                return;

            PlayerController player = GameAccess.Player;
            if (object.ReferenceEquals(player, null))
                return;

            player.SetMoney(__state);
            DamageUpgradeService.ReapplyAfterPlayerReset();
            MLLog.Msg("[Patch][Respawn] Preserved salt after GameManager return to flag: " + __state);
        }
    }
}

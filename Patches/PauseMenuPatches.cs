/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for pause menu additions and return buttons.
 * Why: The mod adds AP-friendly travel actions while avoiding vanilla money-loss respawn behavior.
 * Notes: Menu button patches should tolerate missing UI objects because menu layouts differ between game states.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(GameManager), "Pause")]
    public static class GameManagerPausePatch
    {
        private static void Postfix()
        {
            GreatRuinsReturnService.EnsurePauseMenuButton();
        }
    }

    [HarmonyPatch(typeof(GameManager), "Update")]
    public static class GameManagerPauseMenuUpdatePatch
    {
        private static void Postfix(GameManager __instance)
        {
            if (!object.ReferenceEquals(__instance, null) && __instance.IsPaused())
                GreatRuinsReturnService.EnsurePauseMenuButton();
        }
    }
}

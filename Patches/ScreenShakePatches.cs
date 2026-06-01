/*
 * ArchDandara documentation
 * Purpose: Small patch area for screen shake behavior.
 * Why: Some AP-triggered feedback can call visual code in unusual states, so this keeps it safe.
 * Notes: Keep this patch minimal because screen feedback is cosmetic and should not block gameplay.
 */

using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(ScreenShakeOnDamage), "OnDamage")]
    public static class ScreenShakeOnDamagePatch
    {
        public static bool Prefix()
        {
            return CameraMovement.instance != null;
        }
    }
}

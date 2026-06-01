/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for game proxy objects that grant powerups indirectly.
 * Why: Some vanilla systems unlock through proxy helpers, so this closes bypasses around the main powerup patch.
 * Notes: Proxy patches cover alternate vanilla grant routes that do not call the primary PowerupManager methods directly.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PowerupManagerProxy), "UnlockPowerup")]
    public static class PowerupProxyUnlockPatch
    {
        private static bool Prefix(PowerupManagerProxy __instance)
        {
            return PowerupProxyPatchLogic.Handle(__instance, "UnlockPowerup", "");
        }
    }

    [HarmonyPatch(typeof(PowerupManagerProxy), "UnlockPowerupWithUniqueID")]
    public static class PowerupProxyUnlockWithIdPatch
    {
        private static bool Prefix(PowerupManagerProxy __instance, string id)
        {
            return PowerupProxyPatchLogic.Handle(__instance, "UnlockPowerupWithUniqueID", id);
        }
    }

    [HarmonyPatch(typeof(PowerupManagerProxy), "UnlockUntilMax")]
    public static class PowerupProxyUnlockUntilMaxPatch
    {
        private static bool Prefix(PowerupManagerProxy __instance)
        {
            return PowerupProxyPatchLogic.Handle(__instance, "UnlockUntilMax", "");
        }
    }

    public static class PowerupProxyPatchLogic
    {
        public static bool Handle(PowerupManagerProxy proxy, string methodName, string id)
        {
            if (GrantContext.IsArchipelagoGrant)
                return true;

            if (proxy == null)
                return true;

            string objectName = LocationName.ForObject(proxy, "UNKNOWN_POWERUP_PROXY");
            string uniqueId = !string.IsNullOrEmpty(id) ? id : proxy.uniqueID;
            if (!string.IsNullOrEmpty(uniqueId))
                objectName = objectName + "#" + uniqueId;

            APLocationSender.TrySend("PowerupProxy", GameAccess.CurrentScene, objectName);
            MLLog.Msg("[Patch][PowerupProxy] Blocked vanilla proxy reward: " + methodName + " | " +
                            proxy.toUnlock + " | " + objectName);

            return false;
        }
    }
}

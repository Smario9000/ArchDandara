// PowerupProxylogic.cs
// Logs when a PowerupManagerProxy triggers a reward unlock.

using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(PowerupManagerProxy), "UnlockPowerup")]
    public static class Patch_PowerupProxy_UnlockPowerup
    {
        private static void Prefix(PowerupManagerProxy __instance)
        {
            PowerupProxylogic.LogProxy(__instance, "UnlockPowerup", "");
        }
    }

    [HarmonyPatch(typeof(PowerupManagerProxy), "UnlockPowerupWithUniqueID")]
    public static class Patch_PowerupProxy_UnlockPowerupWithUniqueID
    {
        private static void Prefix(PowerupManagerProxy __instance, string id)
        {
            PowerupProxylogic.LogProxy(__instance, "UnlockPowerupWithUniqueID", id);
        }
    }

    [HarmonyPatch(typeof(PowerupManagerProxy), "UnlockUntilMax")]
    public static class Patch_PowerupProxy_UnlockUntilMax
    {
        private static void Prefix(PowerupManagerProxy __instance)
        {
            PowerupProxylogic.LogProxy(__instance, "UnlockUntilMax", "");
        }
    }

    public static class PowerupProxylogic
    {
        // Shared helper used by all three proxy hook patches.
        public static void LogProxy(PowerupManagerProxy proxy, string sourceMethod, string passedId)
        {
            if (proxy == null)
                return;

            string scene = GetCurrentScene();
            string objectName = proxy.name != null ? proxy.name : "UNKNOWN_PROXY";
            string unlockName = proxy.toUnlock.ToString();

            // Uses passed ID first, then falls back to the proxy's own unique ID.
            string uniqueId = !string.IsNullOrEmpty(passedId)
                ? passedId
                : (!string.IsNullOrEmpty(proxy.uniqueID) ? proxy.uniqueID : "");

            string meta = DataManager.GetRoomMetaText(scene);

            DataManager.LogCheck(
                "PowerupProxy",
                scene,
                objectName,
                unlockName,
                "Source=" + sourceMethod +
                " UniqueID=" + uniqueId +
                (string.IsNullOrEmpty(meta) ? "" : " | " + meta));

            DataManager.LogActivity(
                "PowerupProxy",
                scene,
                objectName,
                unlockName + " | " + sourceMethod + " | " + uniqueId +
                (string.IsNullOrEmpty(meta) ? "" : " | " + meta));

            MelonLogger.Msg(
                "[LOG][PowerupProxy] " +
                scene + " -> " +
                objectName + " -> " +
                unlockName + " | " +
                sourceMethod + " | " +
                uniqueId);
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
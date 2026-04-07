// CheckpointLogic.cs
// Logs when the game discovers a new teleport point / checkpoint.

using System;
using System.Collections.Generic;
using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(TeleportPointManager), "SaveNewTeleportPoint")]
    public static class Checkpointlogic
    {
        // Small cache to stop the same checkpoint from logging repeatedly in a short time.
        private static Dictionary<string, DateTime> _recentTeleportLogs = new Dictionary<string, DateTime>();

        // Returns true if the same teleport log happened too recently.
        private static bool ShouldSkipTeleport(string scene, string type)
        {
            string key = scene + "|" + type;
            DateTime now = DateTime.UtcNow;

            DateTime last;
            if (_recentTeleportLogs.TryGetValue(key, out last))
            {
                if ((now - last).TotalMilliseconds <= 1000)
                    return true;
            }

            _recentTeleportLogs[key] = now;
            return false;
        }

        // Runs after SaveNewTeleportPoint() succeeds.
        private static void Postfix(string sceneName, TeleportPointManager.PointType type, bool __result)
        {
            if (!__result)
                return;

            string currentScene = GetCurrentScene();
            string discoveredScene = string.IsNullOrEmpty(sceneName) ? "UNKNOWN_SCENE" : sceneName;
            string pointType = type.ToString();

            if (ShouldSkipTeleport(discoveredScene, pointType))
                return;

            // Gets extra room/area text for cleaner logs.
            string meta = DataManager.GetRoomMetaText(discoveredScene);

            DataManager.LogCheck(
                "TeleportPointDiscovered",
                currentScene,
                discoveredScene,
                pointType,
                meta);

            DataManager.LogActivity(
                "TeleportPointDiscovered",
                currentScene,
                discoveredScene,
                pointType + (string.IsNullOrEmpty(meta) ? "" : " | " + meta));

            MelonLogger.Msg(
                "[LOG][TeleportPoint] " +
                currentScene + " -> " +
                discoveredScene + " | " +
                pointType +
                (string.IsNullOrEmpty(meta) ? "" : " | " + meta));
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
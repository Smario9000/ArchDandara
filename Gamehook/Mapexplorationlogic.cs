// MapExplorationLogic.cs
// Logs when the map changes to a new room and when a room is newly explored.

using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(Map), "ChangingToScene")]
    public static class MapExplorationLogic
    {
        // Stores state before the room change so we can compare after.
        [System.ThreadStatic]
        private static int _beforeCount;

        [System.ThreadStatic]
        private static string _sceneArg;

        // Runs before Map.ChangingToScene().
        private static void Prefix(Map __instance, string scene)
        {
            _sceneArg = scene ?? "UNKNOWN_SCENE";
            _beforeCount = __instance != null ? __instance.GetNScenesDiscovered() : -1;
        }

        // Runs after Map.ChangingToScene().
        private static void Postfix(Map __instance, string scene, MapRoom __result)
        {
            if (__instance == null)
                return;

            string targetScene = string.IsNullOrEmpty(scene) ? (_sceneArg ?? "UNKNOWN_SCENE") : scene;
            int afterCount = __instance.GetNScenesDiscovered();

            // True if this room increased the discovered-room count.
            bool newlyExplored = _beforeCount >= 0 && afterCount > _beforeCount;

            string roomName = string.Empty;
            string areaName = string.Empty;

            try
            {
                // Prefer the direct returned MapRoom if available.
                if (__result != null)
                {
                    roomName = __result.GetRoomName() ?? string.Empty;
                    areaName = __result.GetAreaName() ?? string.Empty;
                }
                else
                {
                    roomName = DataManager.GetRoomNameForScene(targetScene);
                    areaName = DataManager.GetAreaNameForScene(targetScene);
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning("[MapExplorationLogic] Failed to resolve map room metadata: " + ex.Message);
                roomName = DataManager.GetRoomNameForScene(targetScene);
                areaName = DataManager.GetAreaNameForScene(targetScene);
            }

            string meta = BuildMeta(areaName, roomName, newlyExplored, _beforeCount, afterCount);

            // Always logs room change info to the activity log.
            DataManager.LogActivity(
                "MapChangingToScene",
                targetScene,
                "Map",
                meta);

            // Only logs a check when the room is truly first-time discovered.
            if (newlyExplored)
            {
                DataManager.LogCheck(
                    "MapRoomExplored",
                    targetScene,
                    roomName,
                    areaName,
                    "DiscoveredCount=" + afterCount);

                MelonLogger.Msg(
                    "[LOG][MapExplore] " +
                    targetScene +
                    (string.IsNullOrEmpty(areaName) ? "" : " | Area=" + areaName) +
                    (string.IsNullOrEmpty(roomName) ? "" : " | Room=" + roomName) +
                    " | NewRoom=true | Count=" + afterCount);
            }
            else
            {
                MelonLogger.Msg(
                    "[LOG][MapScene] " +
                    targetScene +
                    (string.IsNullOrEmpty(areaName) ? "" : " | Area=" + areaName) +
                    (string.IsNullOrEmpty(roomName) ? "" : " | Room=" + roomName) +
                    " | NewRoom=false | Count=" + afterCount);
            }
        }

        // Builds one text line that explains the map change state.
        private static string BuildMeta(string areaName, string roomName, bool newlyExplored, int beforeCount, int afterCount)
        {
            string text = "NewRoom=" + newlyExplored +
                          " | Before=" + beforeCount +
                          " | After=" + afterCount;

            if (!string.IsNullOrEmpty(areaName))
                text += " | Area=" + areaName;

            if (!string.IsNullOrEmpty(roomName))
                text += " | Room=" + roomName;

            return text;
        }
    }
}
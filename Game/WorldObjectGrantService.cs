/*
 * ArchDandara documentation
 * Purpose: Applies AP items that modify world objects, such as removing progression blockers.
 * Why: Some AP progression is environmental rather than a player stat and must reapply when rooms reload.
 * Notes: World-object grants should be idempotent because rooms can reload and recreate target objects.
 */

using UnityEngine;

namespace ArchDandara.Game
{
    public static class WorldObjectGrantService
    {
        private const string WallBreakItemName = "Wall Break";
        private const string FortressRoomId = "AF_FinalCamp";
        private const string FortressDoorName = "Fortress Door";

        private static float NextWallBreakCheckTime;
        private static bool LoggedMissingDoor;
        private static bool LoggedWaitingForRoom;
        private static string LastContextKey = "";

        public static bool TryGrant(string itemName)
        {
            if (itemName != WallBreakItemName)
                return false;

            ApplyWallBreak(true);
            return true;
        }

        public static void Update()
        {
            if (SaveSync.GetReceivedItemCount(WallBreakItemName) <= 0)
                return;

            if (Time.time < NextWallBreakCheckTime)
                return;

            NextWallBreakCheckTime = Time.time + 1.0f;
            ApplyWallBreak(false);
        }

        private static void ApplyWallBreak(bool logIfNotInRoom)
        {
            string roomName = GameAccess.CurrentRoomName;
            string sceneName = GameAccess.CurrentScene;
            string contextKey = sceneName + "|" + roomName;
            if (LastContextKey != contextKey)
            {
                LastContextKey = contextKey;
                LoggedMissingDoor = false;
                LoggedWaitingForRoom = false;
            }

            if (!IsFortressRoom(roomName, sceneName))
            {
                if (logIfNotInRoom || !LoggedWaitingForRoom)
                {
                    LoggedWaitingForRoom = true;
                    MLLog.Msg("[WorldObject] Wall Break received. Fortress door will be removed in " +
                                    FortressRoomId + ". Current scene=" + sceneName + ", room=" + roomName + ".");
                }

                return;
            }

            LoggedWaitingForRoom = false;

            GameObject door = FindGameObject(FortressDoorName);
            if (door == null)
            {
                if (logIfNotInRoom && !LoggedMissingDoor)
                {
                    LoggedMissingDoor = true;
                    MLLog.Msg("[WorldObject] Wall Break active, but " + FortressDoorName +
                                    " was not found in " + FortressRoomId + ". Current scene=" + sceneName +
                                    ", room=" + roomName + ".");
                }

                return;
            }

            string doorPath = GetHierarchyPath(door);
            door.SetActive(false);
            Object.Destroy(door);
            LoggedMissingDoor = false;
            MLLog.Msg("[WorldObject] Removed " + doorPath + " in " + FortressRoomId + ".");
        }

        private static bool IsFortressRoom(string roomName, string sceneName)
        {
            return roomName == FortressRoomId || sceneName == FortressRoomId;
        }

        private static GameObject FindGameObject(string objectName)
        {
            GameObject direct = GameObject.Find(objectName);
            if (IsFortressDoorObject(direct))
                return direct;

            Object[] objects = Object.FindObjectsOfType(typeof(GameObject));
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject gameObject = objects[i] as GameObject;
                if (gameObject != null && IsFortressDoorObject(gameObject))
                    return gameObject;
            }

            objects = Resources.FindObjectsOfTypeAll(typeof(GameObject));
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject gameObject = objects[i] as GameObject;
                if (gameObject != null && IsFortressDoorObject(gameObject))
                    return gameObject;
            }

            return null;
        }

        private static bool IsFortressDoorObject(GameObject gameObject)
        {
            if (gameObject == null || string.IsNullOrEmpty(gameObject.name))
                return false;

            return IsFortressDoorName(gameObject.name);
        }

        private static bool IsFortressDoorName(string objectName)
        {
            if (objectName == FortressDoorName)
                return true;

            string normalized = objectName.ToLower();
            normalized = normalized.Replace(" ", "");
            normalized = normalized.Replace("_", "");
            normalized = normalized.Replace("-", "");
            normalized = normalized.Replace("(clone)", "");

            return normalized == "fortressdoor";
        }

        private static string GetHierarchyPath(GameObject gameObject)
        {
            Transform current = gameObject.transform;
            string path = gameObject.name;
            while (current != null && current.parent != null)
            {
                current = current.parent;
                path = current.gameObject.name + "/" + path;
            }

            return path;
        }
    }
}

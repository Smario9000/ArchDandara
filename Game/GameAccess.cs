/*
 * ArchDandara documentation
 * Purpose: Provides safe accessors for common game singletons and current room data.
 * Why: Harmony patches run during many lifecycle states, so null-safe access prevents crashes while managers load.
 * Notes: Use these accessors from patches instead of touching singletons directly when load order is uncertain.
 */

namespace ArchDandara.Game
{
    public static class GameAccess
    {
        private static bool LoggedCurrentRoomFailure;

        public static GameManager GameManager => PersistentSingleton<GameManager>.instance;

        public static PlayerController Player
        {
            get
            {
                GameManager gm = GameManager;
                return gm != null ? gm.GetPlayer() : null;
            }
        }

        public static PowerupManager PowerupManager => PersistentSingleton<PowerupManager>.instance;

        public static StoryManager StoryManager => PersistentSingleton<StoryManager>.instance;

        public static HUDManager HudManager => PersistentSingleton<HUDManager>.instance;

        public static bool IsReadyForApItemGrants
        {
            get
            {
                return SaveSync.IsGameSaveActive &&
                       !object.ReferenceEquals(Player, null) &&
                       !object.ReferenceEquals(PowerupManager, null) &&
                       !object.ReferenceEquals(StoryManager, null);
            }
        }

        public static string CurrentScene
        {
            get
            {
                GameManager gm = GameManager;
                return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
            }
        }

        public static string CurrentRoomName
        {
            get
            {
                try
                {
                    MapManager mapManager = PersistentSingleton<MapManager>.instance;
                    if (!object.ReferenceEquals(mapManager, null) &&
                        !object.ReferenceEquals(mapManager.CurrentMap, null))
                    {
                        MapRoom room = mapManager.CurrentMap.GetCurrentRoom();
                        if (!object.ReferenceEquals(room, null))
                        {
                            string roomName = room.GetRoomName();
                            if (!string.IsNullOrEmpty(roomName))
                                return roomName;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    if (!LoggedCurrentRoomFailure)
                    {
                        LoggedCurrentRoomFailure = true;
                        MLLog.Warning("[GameAccess] Failed to read current map room, falling back to scene: " +
                                            ex.GetType().Name + ": " + ex.Message);
                    }
                }

                return CurrentScene;
            }
        }
    }
}

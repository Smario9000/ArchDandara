/*
 * ArchDandara documentation
 * Purpose: Resolves game interactions into AP location ids and sends checks once.
 * Why: Many game objects report raw room and object names, so this keeps de-duplication and sending in one place.
 * Notes: Send calls mark SaveSync only after the AP client accepts the location, preventing local saves from claiming failed sends.
 */

using ArchDandara.Game;

namespace ArchDandara.Archipelago
{
    public static class APLocationSender
    {
        public static bool TrySend(string sourceType, string sceneName, string objectName)
        {
            string key = LocationKey.Build(sourceType, sceneName, objectName);
            long locationId;

            if (!TryResolveLocationId(sourceType, sceneName, objectName, out locationId))
            {
                MLLog.Msg("[APLocation] Unmapped location: " + key);
                return false;
            }

            return TrySend(locationId, key);
        }

        public static bool TrySend(long locationId, string debugName)
        {
            if (SaveSync.HasCheckedLocation(locationId))
                return false;

            if (!APClient.SendLocation(locationId))
                return false;

            SaveSync.MarkCheckedLocation(locationId);
            SaveSync.Save();

            MLLog.Msg("[APLocation] Checked " + locationId + " | " + debugName);
            return true;
        }

        public static bool IsChecked(string sourceType, string sceneName, string objectName)
        {
            long locationId;
            return TryResolveLocationId(sourceType, sceneName, objectName, out locationId) &&
                   SaveSync.HasCheckedLocation(locationId);
        }

        public static bool TryResolveLocationId(string sourceType, string sceneName, string objectName,
            out long locationId)
        {
            string key = LocationKey.Build(sourceType, sceneName, objectName);
            return LocationIds.TryGetLocationId(key, out locationId) ||
                   LocationIds.TryResolveLocationId(sourceType, sceneName, objectName, out locationId);
        }
    }
}

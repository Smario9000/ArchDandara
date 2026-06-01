/*
 * ArchDandara documentation
 * Purpose: Builds normalized location keys from source type, scene, and object name.
 * Why: Consistent keys let chests, NPCs, altars, and story events share the same lookup path.
 * Notes: Keep normalization conservative; over-normalizing can accidentally merge distinct checks in the same scene.
 */

namespace ArchDandara.Archipelago
{
    public static class LocationKey
    {
        public static string Build(string sourceType, string sceneName, string objectName)
        {
            return Clean(sourceType) + "|" + Clean(sceneName) + "|" + Clean(objectName);
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "UNKNOWN";

            return value.Trim();
        }
    }
}
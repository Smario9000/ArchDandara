/*
 * ArchDandara documentation
 * Purpose: Persists resolved item-location hints for the active player and seed.
 * Why: Blocked interactions should avoid repeatedly spamming the AP server for the same hint information.
 * Notes: Hint cache keys use item names because boss gate UI asks for items, not specific source locations.
 */

using System.Collections.Generic;
using System.IO;
using ArchDandara.Config;

namespace ArchDandara.Archipelago
{
    public static class HintCache
    {
        private static readonly Dictionary<string, APItemLocation> CachedHints =
            new Dictionary<string, APItemLocation>();

        private static string CurrentSeed = "unknown_seed";
        private static string CachePath;

        public static void InitializeForSession(string seedName)
        {
            CachedHints.Clear();

            if (string.IsNullOrEmpty(seedName))
                seedName = "unknown_seed";

            CurrentSeed = seedName;

            string hintFolder = Path.Combine(APConfig.DataFolder, "hint");
            if (!Directory.Exists(hintFolder))
                Directory.CreateDirectory(hintFolder);

            string fileName = CleanFilePart(APConfig.SlotName) + "_" + CleanFilePart(CurrentSeed) + ".txt";
            CachePath = Path.Combine(hintFolder, fileName);
            Load();
            MLLog.Msg("[HintCache] Loaded " + CachedHints.Count + " hints from " + CachePath);
        }

        public static bool TryGet(string itemName, out APItemLocation itemLocation)
        {
            itemLocation = null;
            if (string.IsNullOrEmpty(itemName))
                return false;

            return CachedHints.TryGetValue(itemName, out itemLocation);
        }

        public static void Store(APItemLocation itemLocation)
        {
            if (object.ReferenceEquals(itemLocation, null) || string.IsNullOrEmpty(itemLocation.ItemName))
                return;

            CachedHints[itemLocation.ItemName] = itemLocation;
            Save();
        }

        private static void Load()
        {
            if (string.IsNullOrEmpty(CachePath) || !File.Exists(CachePath))
                return;

            string[] lines = File.ReadAllLines(CachePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('|');
                if (parts.Length < 3)
                    continue;

                string itemName = Unescape(parts[0]);
                string playerName = Unescape(parts[1]);
                string locationName = Unescape(parts[2]);
                if (!string.IsNullOrEmpty(itemName))
                    CachedHints[itemName] = new APItemLocation(itemName, playerName, locationName);
            }
        }

        private static void Save()
        {
            if (string.IsNullOrEmpty(CachePath))
                return;

            string[] lines = new string[CachedHints.Count + 2];
            int index = 0;
            lines[index++] = "# ArchDandara hint cache";
            lines[index++] = "# Slot=" + APConfig.SlotName + " | Seed=" + CurrentSeed;

            foreach (KeyValuePair<string, APItemLocation> pair in CachedHints)
            {
                APItemLocation location = pair.Value;
                lines[index++] = Escape(location.ItemName) + "|" +
                                 Escape(location.PlayerName) + "|" +
                                 Escape(location.LocationName);
            }

            File.WriteAllLines(CachePath, lines);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.Replace("\\", "\\\\")
                .Replace("|", "\\p")
                .Replace("\r", "")
                .Replace("\n", " ");
        }

        private static string Unescape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            string result = "";
            bool escaping = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (escaping)
                {
                    if (c == 'p')
                        result += "|";
                    else
                        result += c;

                    escaping = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaping = true;
                    continue;
                }

                result += c;
            }

            return result;
        }

        private static string CleanFilePart(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "default";

            char[] invalid = Path.GetInvalidFileNameChars();
            string result = value.Trim();
            for (int i = 0; i < invalid.Length; i++)
                result = result.Replace(invalid[i], '_');

            result = result.Replace(' ', '_');
            return result.Length == 0 ? "default" : result;
        }
    }
}

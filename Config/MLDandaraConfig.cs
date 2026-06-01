/*
 * ArchDandara documentation
 * Purpose: Controls MelonLoader console and file logging categories and level overrides.
 * Why: The mod has many diagnostic hooks; category filtering keeps testing readable while allowing deep troubleshooting.
 * Notes: New log categories should default off unless they are required for normal user-facing errors.
 */

using System.Collections.Generic;
using System.IO;
using MelonLoader.Utils;

namespace ArchDandara.Config
{
    public static class MLDandaraConfig
    {
        public static readonly string DataFolder =
            Path.Combine(MelonEnvironment.UserDataDirectory, "ArchDandaraData");

        private static readonly string ConfigPath =
            Path.Combine(DataFolder, "MLDandaraConfig.cfg");

        private static readonly string DebugPath =
            Path.Combine(DataFolder, "Debug.txt");

        private static readonly Dictionary<string, bool> CategoryConsoleEnabled =
            new Dictionary<string, bool>();

        public static bool ConsoleLogsEnabled = true;
        public static bool MsgConsoleEnabled = true;
        public static bool WarningConsoleEnabled = true;
        public static bool ErrorConsoleEnabled = true;
        public static bool CustomConsoleEnabled = true;
        public static bool FileOnlySuppressedLogs = true;

        public static void Initialize()
        {
            ResetDefaults();

            if (!IsDebugConfigEnabled())
                return;

            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            if (!File.Exists(ConfigPath))
                CreateDefaultConfig();

            Load();
        }

        public static void Reload()
        {
            ResetDefaults();

            if (!IsDebugConfigEnabled())
                return;

            if (!File.Exists(ConfigPath))
                CreateDefaultConfig();

            Load();
        }

        public static bool ShouldPrintToConsole(string level, string message)
        {
            if (!ConsoleLogsEnabled)
                return false;

            if (IsLevelEnabled(level))
                return true;

            string[] tags = ExtractTags(message);
            if (tags.Length == 0)
                return IsCategoryEnabled("General");

            for (int count = tags.Length; count > 0; count--)
            {
                string key = JoinTags(tags, count);
                bool enabled;
                if (CategoryConsoleEnabled.TryGetValue(key, out enabled))
                    return enabled;
            }

            return false;
        }

        private static bool IsLevelEnabled(string level)
        {
            switch (level)
            {
                case "Msg":
                    return MsgConsoleEnabled;
                case "Warning":
                    return WarningConsoleEnabled;
                case "Error":
                    return ErrorConsoleEnabled;
                default:
                    return CustomConsoleEnabled;
            }
        }

        private static bool IsCategoryEnabled(string key)
        {
            bool enabled;
            return CategoryConsoleEnabled.TryGetValue(key, out enabled) && enabled;
        }

        private static bool IsDebugConfigEnabled()
        {
            if (!File.Exists(DebugPath))
                return false;

            try
            {
                string text = File.ReadAllText(DebugPath).Trim();
                if (text.Length == 0)
                    return false;

                string[] words = text.Split(new[] { ' ', '\t', '\r', '\n' },
                    System.StringSplitOptions.RemoveEmptyEntries);
                return words.Length > 0 && words[0] == "Debug";
            }
            catch
            {
                return false;
            }
        }

        private static void Load()
        {
            ResetDefaults();

            string[] lines = File.ReadAllLines(ConfigPath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                int split = line.IndexOf('=');
                if (split < 0)
                    continue;

                string key = line.Substring(0, split).Trim();
                string value = line.Substring(split + 1).Trim();
                bool parsed;
                if (!bool.TryParse(value, out parsed))
                    continue;

                switch (key)
                {
                    case "ConsoleLogs":
                        ConsoleLogsEnabled = parsed;
                        break;
                    case "Msg":
                        MsgConsoleEnabled = parsed;
                        break;
                    case "Warning":
                        WarningConsoleEnabled = parsed;
                        break;
                    case "Error":
                        ErrorConsoleEnabled = parsed;
                        break;
                    case "CustomLogs":
                        CustomConsoleEnabled = parsed;
                        break;
                    case "FileOnlySuppressedLogs":
                        FileOnlySuppressedLogs = parsed;
                        break;
                    default:
                        if (key.StartsWith("Category."))
                            CategoryConsoleEnabled[key.Substring("Category.".Length)] = parsed;
                        break;
                }
            }
        }

        private static void ResetDefaults()
        {
            ConsoleLogsEnabled = true;
            MsgConsoleEnabled = false;
            WarningConsoleEnabled = true;
            ErrorConsoleEnabled = true;
            CustomConsoleEnabled = false;
            FileOnlySuppressedLogs = true;

            CategoryConsoleEnabled.Clear();
            CategoryConsoleEnabled["APClient"] = true;
            CategoryConsoleEnabled["APConfig"] = false;
            CategoryConsoleEnabled["APSettings"] = true;
            CategoryConsoleEnabled["Focus"] = false;
            CategoryConsoleEnabled["Keybinds"] = true;
            CategoryConsoleEnabled["MainMenuBranding"] = false;
        }

        private static string[] ExtractTags(string message)
        {
            if (string.IsNullOrEmpty(message) || message[0] != '[')
                return new string[0];

            List<string> tags = new List<string>();
            int index = 0;
            while (index < message.Length && message[index] == '[')
            {
                int close = message.IndexOf(']', index + 1);
                if (close < 0)
                    break;

                string tag = message.Substring(index + 1, close - index - 1).Trim();
                if (tag.Length == 0)
                    break;

                tags.Add(tag);
                index = close + 1;
            }

            return tags.ToArray();
        }

        private static string JoinTags(string[] tags, int count)
        {
            string value = tags[0];
            for (int i = 1; i < count; i++)
                value += "." + tags[i];

            return value;
        }

        private static void CreateDefaultConfig()
        {
            string[] lines =
            {
                "# Dandara MelonLoader Log Console Config",
                "# This controls what ArchDandara prints to the MelonLoader console.",
                "# Suppressed entries are still written to MelonLoader/Latest.log when FileOnlySuppressedLogs=true.",
                "#",
                "# Master override. false hides all ArchDandara console logs.",
                "ConsoleLogs=true",
                "",
                "# Level overrides. true forces that level to print regardless of category.",
                "# false does not block that level; it falls through to Category.* rules.",
                "Msg=false",
                "Warning=true",
                "Error=true",
                "CustomLogs=false",
                "",
                "# Keep suppressed messages in the MelonLoader log file.",
                "FileOnlySuppressedLogs=true",
                "",
                "# Category overrides. These match the leading tags in log messages.",
                "# Nested tags can be controlled with dots, for example:",
                "# Category.Patch.WeaponAltar.A1_GD14=false",
                "Category.APClient=true",
                "Category.APConfig=true",
                "Category.APItemReceiver=false",
                "Category.APLocation=false",
                "Category.APServer=true",
                "Category.APSettings=true",
                "Category.DeathLink=false",
                "Category.Focus=false",
                "Category.GameAccess=false",
                "Category.Gate=false",
                "Category.HintCache=false",
                "Category.HUDRefresh=false",
                "Category.InstallCheck=false",
                "Category.ItemGrant=false",
                "Category.JonnyBMissile=false",
                "Category.Keybinds=true",
                "Category.MainMenuBranding=false",
                "Category.Patch=false",
                "Category.Patch.WeaponAltar.A1_GD14=false",
                "Category.RuntimeLocationResolver=false",
                "Category.SaveProfile=false",
                "Category.SaveSync=false",
                "Category.ShopBar=false",
                "Category.ShopSalt=false",
                "Category.SpecialNPC=false",
                "Category.StoryLocationMap=false",
                "Category.WorldObject=false",
                "Category.General=false"
            };

            File.WriteAllLines(ConfigPath, lines);
        }
    }
}

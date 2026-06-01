/*
 * ArchDandara documentation
 * Purpose: Caches AP slot settings to a readable file inside the active AP save folder.
 * Why: This gives the game a stable local settings copy and lets edits take effect after a controlled restart or reconnect.
 * Notes: The file cache is player-and-seed scoped; do not share settings across seeds because logic and colors may differ.
 */

using System.Collections.Generic;
using System.IO;
using ArchDandara.Game;

namespace ArchDandara.Archipelago
{
    public static class APSlotSettingsFile
    {
        private const string FileName = "APSlotSettings.txt";
        private static string LoadedPath;
        private static IDictionary<string, object> LoadedSettings;

        private static readonly string[] SettingKeys =
        {
            "goal_type",
            "death_link",
            "ammo_mana_cost",
            "shop_cost",
            "salt_drop_multiplier",
            "death_recovery_percent",
            "ap_salt_amount",
            "ap_fear_salt_amount",
            "dandara_arrow_damage_upgrade_amount",
            "dandara_arrow_damage_upgrade_scale",
            "dandara_weapon_damage_upgrade_amount",
            "dandara_weapon_damage_upgrade_scale",
            "salts_awareness_upgrade",
            "salts_awareness_cost_reduction",
            "bought_color",
            "custom_bought_color_r",
            "custom_bought_color_g",
            "custom_bought_color_b",
            "received_color",
            "custom_received_color_r",
            "custom_received_color_g",
            "custom_received_color_b",
            "received_only_color",
            "custom_received_only_color_r",
            "custom_received_only_color_g",
            "custom_received_only_color_b"
        };

        public static IDictionary<string, object> Resolve(IDictionary<string, object> slotData)
        {
            string path = GetPath();
            if (File.Exists(path))
            {
                if (LoadedPath == path && LoadedSettings != null && !IsAtMainMenu())
                    return LoadedSettings;

                IDictionary<string, object> fileSettings = Load(path);
                if (fileSettings.Count > 0)
                {
                    LoadedPath = path;
                    LoadedSettings = fileSettings;
                    MLLog.Msg("[APSettings] Loaded slot settings file: " + path);
                    return fileSettings;
                }

                MLLog.Warning("[APSettings] Slot settings file was empty or invalid, using server slot data: " + path);
                return slotData;
            }

            Write(path, slotData);
            LoadedPath = path;
            LoadedSettings = slotData;
            MLLog.Msg("[APSettings] Created slot settings file: " + path);
            return slotData;
        }

        public static string GetPath()
        {
            APSaveProfileService.EnsureFolder();
            return Path.Combine(APSaveProfileService.GetStorageFolder(), FileName);
        }

        private static IDictionary<string, object> Load(string path)
        {
            Dictionary<string, object> settings = new Dictionary<string, object>();
            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                int equalsIndex = line.IndexOf('=');
                if (equalsIndex <= 0)
                    continue;

                string key = line.Substring(0, equalsIndex).Trim();
                string valueText = line.Substring(equalsIndex + 1).Trim();
                if (key.Length == 0 || valueText.Length == 0)
                    continue;

                int value;
                if (int.TryParse(valueText, out value))
                    settings[key] = value;
            }

            return settings;
        }

        private static void Write(string path, IDictionary<string, object> slotData)
        {
            List<string> lines = new List<string>();
            lines.Add("# ArchDandara AP slot settings");
            lines.Add("# The mod creates this file from server slot data only when it does not exist.");
            lines.Add("# Edit values here, then restart from the main menu for this player and seed.");
            lines.Add("# Format: key=value");
            lines.Add("");

            for (int i = 0; i < SettingKeys.Length; i++)
            {
                string key = SettingKeys[i];
                object value;
                if (slotData == null || !slotData.TryGetValue(key, out value) || value == null)
                    continue;

                int intValue;
                if (!TryToInt(value, out intValue))
                    continue;

                lines.Add(key + "=" + intValue);
            }

            File.WriteAllLines(path, lines.ToArray());
        }

        private static bool TryToInt(object value, out int intValue)
        {
            if (value is int)
            {
                intValue = (int)value;
                return true;
            }

            if (value is long)
            {
                intValue = (int)(long)value;
                return true;
            }

            if (value is short)
            {
                intValue = (short)value;
                return true;
            }

            if (value is byte)
            {
                intValue = (byte)value;
                return true;
            }

            return int.TryParse(value.ToString(), out intValue);
        }

        private static bool IsAtMainMenu()
        {
            return object.ReferenceEquals(GameAccess.Player, null);
        }
    }
}

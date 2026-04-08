using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MelonLoader;
using MelonLoader.Utils;

namespace ArchDandara
{
    public static class SkipCutsceneConfig
    {
        private static string _configPath = string.Empty;

        public static bool Enabled = true;
        public static string StartCampScene = "A1_ForestEdge";
        public static bool VerboseDebug = true;
        public static bool SeedIntroState = true;
        public static bool AutoActivateGreatRuinsTutorialSwitches = true;

        public static void Init()
        {
            _configPath = Path.Combine(MelonEnvironment.UserDataDirectory, "SkipCutscene.cfg");

            if (!File.Exists(_configPath))
            {
                WriteDefaultFile();
                MelonLogger.Msg("[SkipCutsceneConfig] Created config: " + _configPath);
            }

            Load();
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(_configPath))
                    WriteDefaultFile();

                string[] lines = File.ReadAllLines(_configPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    string raw = lines[i];
                    if (string.IsNullOrEmpty(raw))
                        continue;

                    string line = raw.Trim();

                    if (line.Length == 0)
                        continue;

                    if (line.StartsWith("#") || line.StartsWith("//") || line.StartsWith(";"))
                        continue;

                    int equalsIndex = line.IndexOf('=');
                    if (equalsIndex <= 0)
                        continue;

                    string key = line.Substring(0, equalsIndex).Trim();
                    string value = line.Substring(equalsIndex + 1).Trim();

                    ApplyValue(key, value);
                }

                MelonLogger.Msg(
                    "[SkipCutsceneConfig] Loaded | " +
                    "Enabled=" + Enabled +
                    " StartCampScene=" + StartCampScene +
                    " VerboseDebug=" + VerboseDebug +
                    " SeedIntroState=" + SeedIntroState +
                    " AutoActivateGreatRuinsTutorialSwitches=" + AutoActivateGreatRuinsTutorialSwitches);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[SkipCutsceneConfig] Failed to load config: " + ex);
            }
        }

        private static void ApplyValue(string key, string value)
        {
            string lowerKey = key.ToLowerInvariant();

            if (lowerKey == "enabled")
            {
                Enabled = ParseBool(value, Enabled);
                return;
            }

            if (lowerKey == "startcampscene")
            {
                if (!string.IsNullOrEmpty(value))
                    StartCampScene = value;
                return;
            }

            if (lowerKey == "verbosedebug")
            {
                VerboseDebug = ParseBool(value, VerboseDebug);
                return;
            }

            if (lowerKey == "seedintrostate")
            {
                SeedIntroState = ParseBool(value, SeedIntroState);
                return;
            }

            if (lowerKey == "autoactivategreatruinstutorialswitches")
            {
                AutoActivateGreatRuinsTutorialSwitches = ParseBool(value, AutoActivateGreatRuinsTutorialSwitches);
            }
        }

        private static bool ParseBool(string value, bool fallback)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;

            string lower = value.Trim().ToLowerInvariant();

            if (lower == "true" || lower == "1" || lower == "yes" || lower == "on")
                return true;

            if (lower == "false" || lower == "0" || lower == "no" || lower == "off")
                return false;

            return fallback;
        }
        
        public static void Reload()
        {
            try
            {
                ResetToDefaults();
                Load();

                MelonLogger.Msg("[SkipCutsceneConfig] Reloaded from file.");
                MelonLogger.Msg(
                    "[SkipCutsceneConfig] Current values | " +
                    "Enabled=" + Enabled +
                    " StartCampScene=" + StartCampScene +
                    " VerboseDebug=" + VerboseDebug +
                    " SeedIntroState=" + SeedIntroState +
                    " AutoActivateGreatRuinsTutorialSwitches=" + AutoActivateGreatRuinsTutorialSwitches);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[SkipCutsceneConfig] Reload failed: " + ex);
            }
        }

        private static void ResetToDefaults()
        {
            Enabled = true;
            StartCampScene = "A1_ForestEdge";
            VerboseDebug = true;
            SeedIntroState = true;
            AutoActivateGreatRuinsTutorialSwitches = true;
        }

        private static void WriteDefaultFile()
        {
            string text =
                "# SkipCutscene.cfg\r\n" +
                "# Controls fresh New Game intro skipping.\r\n" +
                "# Edit this file, then restart the game.\r\n" +
                "#\r\n" +
                "# ------------------------------\r\n" +
                "# Enabled\r\n" +
                "# ------------------------------\r\n" +
                "# true  = skip intro/start in custom room\r\n" +
                "# false = use normal game intro\r\n" +
                "Enabled=true\r\n\r\n" +

                "# ------------------------------\r\n" +
                "# StartCampScene\r\n" +
                "# ------------------------------\r\n" +
                "# Camp scene to start in when skipping the intro.\r\n" +
                "# Only use scenes that exist in camps.txt or were logged as Camp/Flag discoveries.\r\n" +
                "# Known camp scenes found from your current files:\r\n" +
                BuildCommentList(GetKnownCampScenes()) +
                "StartCampScene=A1_ForestEdge\r\n\r\n" +

                "# ------------------------------\r\n" +
                "# VerboseDebug\r\n" +
                "# ------------------------------\r\n" +
                "# true  = print extra debug info to console/log\r\n" +
                "# false = only print important messages\r\n" +
                "VerboseDebug=true\r\n\r\n" +

                "# ------------------------------\r\n" +
                "# SeedIntroState\r\n" +
                "# ------------------------------\r\n" +
                "# true  = marks intro story flags as already completed\r\n" +
                "# false = only changes starting room\r\n" +
                "SeedIntroState=true\r\n\r\n" +

                "# ------------------------------\r\n" +
                "# AutoActivateGreatRuinsTutorialSwitches\r\n" +
                "# ------------------------------\r\n" +
                "# true  = auto-turn on the two Great Ruins tutorial levers\r\n" +
                "# false = leave them normal\r\n" +
                "AutoActivateGreatRuinsTutorialSwitches=true\r\n";

            File.WriteAllText(_configPath, text);
        }
        private static List<string> GetKnownCampScenes()
        {
            List<string> camps = new List<string>();

            try
            {
                string docFolder = Path.Combine(MelonEnvironment.GameRootDirectory, "doc");
                string path = Path.Combine(docFolder, "camps.txt");

                if (!File.Exists(path))
                {
                    AddUniqueScene(camps, "A1_ForestEdge");
                    camps.Sort(StringComparer.OrdinalIgnoreCase);
                    return camps;
                }

                string[] lines = File.ReadAllLines(path);
                string currentScene = string.Empty;

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    if (line.StartsWith("The Camp: ", StringComparison.OrdinalIgnoreCase))
                    {
                        currentScene = line.Substring("The Camp: ".Length).Trim();
                        continue;
                    }

                    if (line.StartsWith("Camp -> ", StringComparison.OrdinalIgnoreCase))
                    {
                        string[] parts = line.Split(new[] { "->" }, StringSplitOptions.None);

                        if (parts.Length >= 2)
                        {
                            string pointType = parts[1].Trim();

                            if ((pointType == "Camp" || pointType == "Flag") &&
                                LooksLikeSceneId(currentScene))
                            {
                                AddUniqueScene(camps, currentScene);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Warning("[SkipCutsceneConfig] Failed to read camps.txt: " + ex.Message);
            }

            if (camps.Count == 0)
                AddUniqueScene(camps, "A1_ForestEdge");

            camps.Sort(StringComparer.OrdinalIgnoreCase);
            return camps;
        }

        private static void AddUniqueScene(List<string> scenes, string scene)
        {
            if (string.IsNullOrEmpty(scene))
                return;

            for (int i = 0; i < scenes.Count; i++)
            {
                if (string.Equals(scenes[i], scene, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            scenes.Add(scene);
        }

        private static bool LooksLikeSceneId(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            // Very loose filter for Dandara scene IDs like A1_ForestEdge, AB_FirstTrack1, A0_Tutorial1
            char first = value[0];
            if (first != 'A')
                return false;

            return value.Contains("_");
        }

        private static string BuildCommentList(List<string> values)
        {
            if (values == null || values.Count == 0)
                return "# (none found)\r\n";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int i = 0; i < values.Count; i++)
            {
                sb.Append("# ");
                sb.Append(values[i]);
                sb.Append("\r\n");
            }

            return sb.ToString();
        }
    }
}
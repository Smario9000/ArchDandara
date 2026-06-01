/*
 * ArchDandara documentation
 * Purpose: Reads and writes the player AP connection config.
 * Why: The mod needs a simple source of truth for server, port, slot, password, and manual reconnect behavior.
 * Notes: The config file is intentionally simple key/value text so the in-game menu and manual edits use the same format.
 */

using System.IO;
using MelonLoader.Utils;

namespace ArchDandara.Config
{
    public static class APConfig
    {
        // =========================
        // Paths
        // =========================

        public static readonly string DataFolder =
            Path.Combine(MelonEnvironment.UserDataDirectory, "ArchDandaraData");

        public static readonly string SaveFolder =
            Path.Combine(DataFolder, "save");

        private static readonly string ConfigPath =
            Path.Combine(DataFolder, "APDandaraConfig.cfg");

        public static string FilePath
        {
            get { return ConfigPath; }
        }

        // =========================
        // Config Values
        // =========================

        public static string ServerAddress = "localhost";
        public static int ServerPort = 38281;

        public static string SlotName = "Player1";
        public static string Password = "";
        private static bool _autoConnect = false;
        private static string _lastLoadSummary = "Config has not been loaded yet.";

        // =========================
        // Initialize
        // =========================

        public static void Initialize()
        {
            CreateFolders();

            if (!File.Exists(ConfigPath)) CreateDefaultConfig();

            LoadConfig();
        }

        public static void Reload()
        {
            ServerAddress = "localhost";
            ServerPort = 38281;
            SlotName = "DandaraPlayer1";
            Password = "";
            _autoConnect = false;

            if (!File.Exists(ConfigPath)) CreateDefaultConfig();

            LoadConfig();
            MLLog.Msg("[APConfig] Reloaded config.");
        }

        public static void Print()
        {
            MLLog.Msg("[APConfig] " + _lastLoadSummary);
        }

        public static void SaveAndReload(string serverAddress, int serverPort, string slotName, string password,
            bool autoConnect)
        {
            if (!Directory.Exists(DataFolder))
                Directory.CreateDirectory(DataFolder);

            string[] lines =
            {
                "# Dandara Archipelago Config",
                "# Server Address, use localhost to host Local with port 38281",
                "ServerAddress= " + (string.IsNullOrEmpty(serverAddress) ? "localhost" : serverAddress),
                "ServerPort= " + serverPort,
                "#SlotName is for the name of the player that you want to connect to.",
                "SlotName= " + (string.IsNullOrEmpty(slotName) ? "DandaraPlayer1" : slotName),
                "# Password is used if the room as one, leave empty if you there is none.",
                "Password=" + (password ?? ""),
                "# AutoConnect is ignored by the mod. Press F3 in-game to connect.",
                "AutoConnect=" + autoConnect
            };

            File.WriteAllLines(ConfigPath, lines);
            Reload();
        }

        // =========================
        // Create Folder
        // =========================

        private static void CreateFolders()
        {
            if (!Directory.Exists(DataFolder))
            {
                Directory.CreateDirectory(DataFolder);

                MLLog.Msg($"[APConfig] Created Folder: {DataFolder}");
            }

            if (!Directory.Exists(SaveFolder))
            {
                Directory.CreateDirectory(SaveFolder);

                MLLog.Msg($"[APConfig] Created Save Folder: {SaveFolder}");
            }
        }

        // =========================
        // Create Config
        // =========================

        private static void CreateDefaultConfig()
        {
            string[] lines =
            {
                "# Dandara Archipelago Config",
                "# Server Address, use localhost to host Local with port 38281",
                "ServerAddress= localhost",
                "ServerPort= 38281",
                "#SlotName is for the name of the player that you want to connect to.",
                "SlotName= DandaraPlayer1",
                "# Password is used if the room as one, leave empty if you there is none.",
                "Password=",
                "# AutoConnect is ignored by the mod. Press F3 in-game to connect.",
                "AutoConnect=false"
            };

            File.WriteAllLines(ConfigPath, lines);

            MLLog.Msg("[APConfig] Created Default Config: " + ConfigPath);
        }

        // =========================
        // Load Config
        // =========================

        private static void LoadConfig()
        {
            string[] lines = File.ReadAllLines(ConfigPath);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                // Ignore comments / blank lines
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#"))
                    continue;

                string[] split = line.Split('=');

                if (split.Length < 2)
                    continue;

                string key = split[0].Trim();
                string value = split[1].Trim();

                switch (key)
                {
                    case "ServerAddress":
                        if (!string.IsNullOrEmpty(value))
                            ServerAddress = value;
                        break;

                    case "ServerPort":
                        int port;
                        if (int.TryParse(value, out port))
                            ServerPort = port;
                        break;

                    case "SlotName":
                        if (!string.IsNullOrEmpty(value))
                            SlotName = value;
                        break;

                    case "Password":
                        Password = value;
                        break;

                    case "AutoConnect":
                        bool autoConnect;
                        if (bool.TryParse(value, out autoConnect))
                            _autoConnect = autoConnect;
                        break;
                }
            }

            MLLog.Msg("[APConfig] Config Loaded");

            MLLog.Msg($"[APConfig] Server: {ServerAddress}:{ServerPort}");
            MLLog.Msg($"[APConfig] Slot: {SlotName}");
            _lastLoadSummary = "Path=" + ConfigPath +
                              " | Server=" + ServerAddress + ":" + ServerPort +
                              " | Slot=" + SlotName +
                              " | PasswordSet=" + (!string.IsNullOrEmpty(Password)) +
                              " | AutoConnectIgnored=" + _autoConnect;
        }
    }
}

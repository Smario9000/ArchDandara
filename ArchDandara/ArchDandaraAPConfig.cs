//ArchDandaraAPConfig.cs

using MelonLoader;
using MelonLoader.Utils;
using System.IO;

namespace ArchDandara
{
    public class ArchDandaraAPConfig :  MelonLogger
    {
        private static string ServerAddress { get; set; }
        private static int Port { get; set; }
        private static string PlayerName { get; set; }
        private static string Password { get; set; }

        private static ConfigFile _file;

        private static void Print(string msg, int level = 1)
        {
            if (!ArchDandaraConfig.LogAPDebug)
                return; // logging disabled
            
            switch (level)
            {
                case 1:
                    MelonLogger.Msg("[Archipelago] " + msg);
                    break;

                case 2:
                    MelonLogger.Warning("[Archipelago] " + msg);
                    break;

                case 3:
                    MelonLogger.Error("[Archipelago] " + msg);
                    break;
            }
        }
        // =====================================================================
        // CONSTRUCTOR — creates folder + config file object
        // (Does NOT load values until Load() is manually called)
        // =====================================================================
        public ArchDandaraAPConfig()
        {
            string folder = Path.Combine(MelonEnvironment.UserDataDirectory, "ArchDandara");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string cfgPath = Path.Combine(folder, "ArchDandaraAP.cfg");

            Print("Loading cfg from: " + cfgPath);

            _file = new ConfigFile(cfgPath);
            Load();
        }

        // =====================================================================
        // LOAD — Reads values from .cfg file (creates missing keys)
        // =====================================================================
        public static void Load()
        {
            if (_file == null)
                return;
            
            ServerAddress = _file.Get("ServerAddress", "localhost");
            Port          = _file.GetInt("Port", 38281);
            PlayerName    = _file.Get("PlayerName", "Player");
            Password      = _file.Get("Password", "");

            Save(); // optional but ensures new keys get written to disk
        }

        // =====================================================================
        // SAVE — Writes updated values back to the .cfg file
        // =====================================================================
        private static void Save()
        {
            _file.Set("ServerAddress", ServerAddress);
            _file.Set("Port", Port);
            _file.Set("PlayerName", PlayerName);
            _file.Set("Password", Password);

            _file.Save();
        }

        // =====================================================================
        // OPTIONAL: Methods to update values safely
        // =====================================================================
        public void SetServer(string value)
        {
            ServerAddress = value;
            Save();
        }

        public void SetPort(int value)
        {
            Port = value;
            Save();
        }

        public void SetPlayerName(string value)
        {
            PlayerName = value;
            Save();
        }

        public void SetPassword(string value)
        {
            Password = value;
            Save();
        }
    }
}
//ArchDandaraAPConfig.cs

using MelonLoader;
using MelonLoader.Utils;
using System.IO;

namespace ArchDandara
{
    /* ============================================================================================================
     *  ArchDandaraAPConfig
     * ------------------------------------------------------------------------------------------------------------
     *  PURPOSE:
     *      This class manages a standalone configuration file specifically for Archipelago network settings.
     *      Unlike ArchDandaraConfig (general mod options), this file stores:
     *          • Server address
     *          • Port
     *          • Player name
     *          • Password
     *
     *  DESIGN OVERVIEW:
     *      • Uses *ConfigFile.cs* for loading + saving instead of MelonPreferences.
     *      • Automatically creates folder:
     *            UserData/ArchDandara/ArchDandaraAP.cfg
     *      • All values load through Load() and automatically saved via Save().
     * ============================================================================================================*/

    public class ArchDandaraAPConfig : MelonLogger
    {
        // ========================================================================================================
        //  CONFIG VALUES
        // --------------------------------------------------------------------------------------------------------
        //  These hold the loaded configuration results. Using static ensures the settings are global to the mod.
        // ========================================================================================================

        private static string ServerAddress { get; set; }  
        private static int    Port          { get; set; }  
        private static string PlayerName    { get; set; }  
        private static string Password      { get; set; }  

        // The ConfigFile object that handles disk IO for this .cfg file.
        private static ConfigFile _file;
        
        // ========================================================================================================
        //  PRINT — Logging helper
        // --------------------------------------------------------------------------------------------------------
        //      This method prints logs only when AP debugging is enabled.  
        //      Uses three levels of severity:
        //          • level 1 → normal message
        //          • level 2 → warning
        //          • level 3 → error
        //      All output is wrapped in "[Archipelago]" prefix to allow filtering in MelonLoader.
        // ========================================================================================================
        private static void Print(string msg, int level = 1)
        {
            if (!ArchDandaraConfig.LogAPDebug)
                return; // Logging disabled globally for AP-related messages.

            switch (level)
            {
                case 1:
                    Msg("[Archipelago] " + msg);
                    break;

                case 2:
                    Warning("[Archipelago] " + msg);
                    break;

                case 3:
                    Error("[Archipelago] " + msg);
                    break;
            }
        }
        
        // ========================================================================================================
        //  CONSTRUCTOR — Sets up the AP config file
        // --------------------------------------------------------------------------------------------------------
        //      The constructor is responsible for:
        //          1. Ensuring the folder exists
        //          2. Creating the ConfigFile object
        //          3. Calling Load() to import values
        //
        //  IMPORTANT:
        //      The config does NOT load automatically by having static fields — you MUST instantiate this class
        //      in MainMod.OnInitializeMelon or call Load() manually.
        //      Path: UserData/ArchDandara/ArchDandaraAP.cfg
        // ========================================================================================================
        public static void Init()
        {
            string folder = Path.Combine(MelonEnvironment.UserDataDirectory, "ArchDandara");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string cfgPath = Path.Combine(folder, "ArchDandaraAP.cfg");

            Print("Loading cfg from: " + cfgPath);

            _file = new ConfigFile(cfgPath);

            // Load config values into static properties
            Load();
        }
        
        // ========================================================================================================
        //  LOAD — Import values from file
        // --------------------------------------------------------------------------------------------------------
        //      Loads each key from ArchDandaraAP.cfg.
        //      If a key is missing:
        //          → A default value is written into the ConfigFile instance.
        //          → Save() writes missing defaults back to disk.
        //      Called automatically by constructor.
        // ========================================================================================================
        private static void Load()
        {
            if (_file == null)
                return; // Prevent null crash if Load() called too early.

            ServerAddress = _file.Get("ServerAddress", "localhost");
            Port          = _file.GetInt("Port", 38281);
            PlayerName    = _file.Get("PlayerName", "Player");
            Password      = _file.Get("Password", "");

            // Save ensures any missing keys get written immediately.
            Save();
        }
        
        // ========================================================================================================
        //  SAVE — Commit all values to disk
        // --------------------------------------------------------------------------------------------------------
        //      Writes all configuration settings into ArchDandaraAP.cfg.
        //      Save() must be called whenever a value is updated.
        //      Relies on ConfigFile.Set() and ConfigFile.Save().
        // ========================================================================================================
        private static void Save()
        {
            _file.Set("ServerAddress", ServerAddress);
            _file.Set("Port", Port);
            _file.Set("PlayerName", PlayerName);
            _file.Set("Password", Password);

            _file.Save();
        }
        
        // ========================================================================================================
        //  MODIFYING CONFIG VALUES — Safe setters
        // --------------------------------------------------------------------------------------------------------
        //      Changing a value automatically writes to disk.
        //      These are convenience wrappers for external UI or network logic.
        // ========================================================================================================
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
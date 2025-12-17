//ArchDandaraConfig.cs

using System.IO;
using MelonLoader.Utils;

namespace ArchDandara
{
    public class ArchDandaraConfig
    {
        // ============================================================================================
        //  FILE PATH FIELDS
        // --------------------------------------------------------------------------------------------
        //  _dir   → Directory created under UserData\ArchDandara
        //  _file  → Full path to config file (ArchDandara.cfg)
        //  _config → The ConfigFile loader/writer handling raw text I/O
        //
        //  These variables are set once inside Init() and reused forever.
        // ============================================================================================
        private static string _dir;
        private static string _file;
        private static ConfigFile _config;

        // ============================================================================================
        //  PUBLIC CONFIG FLAGS
        // --------------------------------------------------------------------------------------------
        //  These are the values game code reads while running.
        //  They are populated immediately after Init() is called.
        //
        //  • EnableRoomScanning — Turns the scanner system on/off
        //  • LogDebugPatch — Enables Harmony patch log output
        //  • LogDoorJsonManager — Controls JSON manager logs
        //  • LogRoomDoorScanner — Logs inside the scanner behavior
        //  • LogArchipelago — Future AP integration use
        //  • LogAPDebug — Debug logs from Archipelago layer
        // ============================================================================================
        public static bool EnableRoomScanning { get; private set; }
        public static bool DoorDatabaseReadonly { get; private set; }
        public static bool LogArchipelago { get; private set; }
        public static bool LogAPDebug { get; private set; }
        public static bool LogDebugPatch { get; private set; }
        public static bool LogDoorJsonManager { get; private set; }
        public static bool LogRoomDoorScanner { get; private set; }
        public static bool LogDoorRandomizer { get; private set; }
        public static bool LogMoneyPatch { get; private set; }
        
        
        // ============================================================================================
        //  INIT() — FIRST-TIME INITIALIZATION (Training Manual Style)
        // --------------------------------------------------------------------------------------------
        //  This method MUST be called once from MainMod.OnInitializeMelon().
        //  It sets up the folder + config file, loads user values, and writes
        //  missing defaults back into the file.
        //
        //  Steps it performs:
        //   1. Create …\UserData\ArchDandara folder if missing
        //   2. Point _file to ArchDandara.cfg inside that folder
        //   3. Create ConfigFile wrapper around that file
        //   4. Load existing config values into _config._values
        //   5. Read values into our strongly-typed static fields
        //   6. Save final version to ensure defaults appear on disk
        // ============================================================================================
        public static void Init()
        {
            // 1 — Build directory path UserData/ArchDandara
            _dir = Path.Combine(MelonEnvironment.UserDataDirectory, "ArchDandara");
            if (!Directory.Exists(_dir))
                Directory.CreateDirectory(_dir);

            // 2 — Full path to ArchDandara.cfg
            _file = Path.Combine(_dir, "ArchDandara.cfg");

            // 3 — Load using our simple config system
            _config = new ConfigFile(_file);

            // 4 — Load file if it exists
            _config.Load();

            // 5 — Move values from text file → static booleans
            LoadSettings();

            // 6 — Save them back so missing keys appear on disk
            Save();
        }

        // ============================================================================================
        //  LoadSettings() — (Professional Documentation Style)
        // --------------------------------------------------------------------------------------------
        //  Reads boolean values from config file into fields.
        //  Missing values are created automatically by ConfigFile.GetBool().
        // ============================================================================================
        private static void LoadSettings()
        {
            EnableRoomScanning   = _config.GetBool("EnableRoomScanning", true);
            DoorDatabaseReadonly = _config.GetBool("DoorDatabaseReadonly", true);
                
            LogArchipelago       = _config.GetBool("LogArchipelago", true);
            LogAPDebug           = _config.GetBool("LogAPDebug", true);
            LogDebugPatch        = _config.GetBool("LogDebugPatch", true);
            LogDoorJsonManager   = _config.GetBool("LogDoorJsonManager", true);
            LogDoorRandomizer    = _config.GetBool("LogDoorRandomizer", true); 
            LogRoomDoorScanner   = _config.GetBool("LogRoomDoorScanner", true);
            LogMoneyPatch          = _config.GetBool("LogMoneyPatch", true);
            
        }

        // ============================================================================================
        //  Writes all current config values back to disk as text.
        //  Also adds a large readable header explaining the file.
        //
        //  Useful because:
        //   • Players can easily change mod behavior without coding
        //   • New settings automatically appear after updates
        //   • Comments help future maintainers understand purpose
        // ============================================================================================
        private static void Save()
        {
            // Write values into ConfigFile's dictionary
            _config.Set("EnableRoomScanning",   EnableRoomScanning);
            _config.Set("DoorDatabaseReadonly", DoorDatabaseReadonly);
            
            _config.Set("LogArchipelago",      LogArchipelago);
            _config.Set("LogAPDebug",          LogAPDebug);
            _config.Set("LogDebugPatch",       LogDebugPatch);
            _config.Set("LogDoorJsonManager",  LogDoorJsonManager);
            _config.Set("LogDoorRandomizer",   LogDoorRandomizer);
            _config.Set("LogRoomDoorScanner",  LogRoomDoorScanner);
            _config.Set("LogStopSave",         LogMoneyPatch);

            // Human-readable header
            string header =
@"# ============================================================================================
#  ArchDandara.cfg — Configuration for the ArchDandara Mod
# --------------------------------------------------------------------------------------------
#  • Controls debug logging output (enable/disable logging categories)
#  • Controls whether RoomDoorScanner runs and updates JSON
#  • This file is automatically created and managed by the ArchDandara Mod
#  • Edit values using: Key= True (on) | False (off)
# ============================================================================================
";
            // Write file to disk
            _config.Save(header);
        }
    }
}
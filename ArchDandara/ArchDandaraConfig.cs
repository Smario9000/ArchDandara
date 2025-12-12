//ArchDandaraConfig.cs

using System.IO;
using MelonLoader.Utils;

namespace ArchDandara
{
    public class ArchDandaraConfig
    {
        private static string _dir;
        private static string _file;
        private static ConfigFile _config;

        // ============================================================
        //  PUBLIC SETTINGS
        // ============================================================
        public static bool EnableRoomScanning { get; private set; }

        public static bool LogDebugPatch { get; private set; }
        public static bool LogDoorJsonManager { get; private set; }
        public static bool LogRoomDoorScanner { get; private set; }
        public static bool LogArchipelago { get; private set; }
        public static bool LogAPDebug { get; private set; }

        // ============================================================
        //  INITIALIZE (called manually from MainMod.OnInitializeMelon)
        // ============================================================
        public static void Init()
        {
            _dir = Path.Combine(MelonEnvironment.UserDataDirectory, "ArchDandara");
            if (!Directory.Exists(_dir))
                Directory.CreateDirectory(_dir);

            _file = Path.Combine(_dir, "ArchDandara.cfg");

            _config = new ConfigFile(_file);

            // Load if exists
            _config.Load();

            // Load settings into variables
            LoadSettings();

            // Write back (in case defaults were added)
            Save();
        }

        // ============================================================
        // LOAD INTO VARIABLES
        // ============================================================
        private static void LoadSettings()
        {
            EnableRoomScanning = _config.GetBool("EnableRoomScanning", true);

            LogDebugPatch       = _config.GetBool("LogDebugPatch", true);
            LogDoorJsonManager  = _config.GetBool("LogDoorJsonManager", true);
            LogRoomDoorScanner  = _config.GetBool("LogRoomDoorScanner", true);
            LogArchipelago      = _config.GetBool("LogArchipelago", true);
            LogAPDebug          = _config.GetBool("LogAPDebug", true);
        }

        // ============================================================
        // SAVE BACK TO FILE
        // ============================================================
        private static void Save()
        {
            // Write values back
            _config.Set("EnableRoomScanning", EnableRoomScanning);

            _config.Set("LogDebugPatch", LogDebugPatch);
            _config.Set("LogDoorJsonManager", LogDoorJsonManager);
            _config.Set("LogRoomDoorScanner", LogRoomDoorScanner);
            _config.Set("LogArchipelago", LogArchipelago);
            _config.Set("LogAPDebug", LogAPDebug);

            string header =
@"# ============================================================================================
#  ArchDandara.cfg — Configuration for the ArchDandara Mod
# --------------------------------------------------------------------------------------------
#  • Controls debug logging output (enable/disable logging categories)
#  • Controls whether RoomDoorScanner runs and updates JSON
#  • This file is automatically created and managed by the ArchDandara Mod
#  • Edit values as: key=values
# ============================================================================================
";
            _config.Save(header);
        }
    }
}
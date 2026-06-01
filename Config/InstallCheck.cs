/*
 * ArchDandara documentation
 * Purpose: Checks whether required mod, UserLibs, and bridge tool files are installed.
 * Why: Missing runtime DLLs fail late and unclearly in MelonLoader, so startup validation gives direct repair info.
 * Notes: Install validation should report missing files only; it should not try to download or mutate user installs.
 */

using System.IO;
using MelonLoader.Utils;

namespace ArchDandara.Config
{
    public static class InstallCheck
    {
        private static readonly string[] RequiredMods =
        {
            "Archipelago.MultiClient.Net.dll",
            "Newtonsoft.Json.dll",
            "websocket-sharp.dll"
        };

        private static readonly string[] RequiredUserLibs =
        {
            "System.Data.dll",
            "System.Runtime.Serialization.dll"
        };

        private static readonly string[] RequiredTools =
        {
            "ArchipelagoWssBridge.exe",
            "websocket-sharp.dll"
        };

        public static void Run()
        {
            string gameFolder = GetGameFolder();
            if (string.IsNullOrEmpty(gameFolder))
            {
                MLLog.Error("[InstallCheck] Could not resolve Dandara game folder.");
                return;
            }

            CheckRequiredFiles(Path.Combine(gameFolder, "Mods"), RequiredMods, "Mods");
            CheckRequiredFiles(Path.Combine(gameFolder, "UserLibs"), RequiredUserLibs, "UserLibs");
            string userDataFolder = Path.Combine(gameFolder, "UserData");
            string archDataFolder = Path.Combine(userDataFolder, "ArchDandaraData");
            string toolsFolder = Path.Combine(archDataFolder, "Tools");
            CheckRequiredFiles(toolsFolder, RequiredTools, "ArchDandaraData Tools");
        }

        private static void CheckRequiredFiles(string folder, string[] requiredFiles, string folderName)
        {
            if (!Directory.Exists(folder))
            {
                MLLog.Error("[InstallCheck] Missing " + folderName + " folder: " + folder);
                return;
            }

            for (int i = 0; i < requiredFiles.Length; i++)
            {
                string path = Path.Combine(folder, requiredFiles[i]);
                if (!File.Exists(path))
                    MLLog.Error("[InstallCheck] Missing required " + folderName + " file: " + path);
            }
        }

        private static string GetGameFolder()
        {
            string userData = MelonEnvironment.UserDataDirectory;
            if (string.IsNullOrEmpty(userData))
                return null;

            DirectoryInfo userDataDirectory = new DirectoryInfo(userData);
            DirectoryInfo gameDirectory = userDataDirectory.Parent;
            return gameDirectory == null ? null : gameDirectory.FullName;
        }
    }
}

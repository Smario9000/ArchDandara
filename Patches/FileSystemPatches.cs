/*
 * ArchDandara documentation
 * Purpose: Redirects game save paths into AP-specific profile folders.
 * Why: AP seeds and slots need separate saves so sessions do not overwrite each other.
 * Notes: Path redirects should happen before the game opens save files, otherwise old default saves may leak in.
 */

using System.IO;
using ArchDandara.Game;
using Dandara.Save;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(FileSystemUnityDefault), "HasFile")]
    public static class FileSystemUnityDefaultHasFilePatch
    {
        private static bool Prefix(string fileName, ref bool __result)
        {
            string path = APSaveProfileService.GetGameSavePath(fileName);
            __result = File.Exists(path);
            return false;
        }
    }

    [HarmonyPatch(typeof(FileSystemUnityDefault), "Delete")]
    public static class FileSystemUnityDefaultDeletePatch
    {
        private static bool Prefix(string fileName)
        {
            string path = APSaveProfileService.GetGameSavePath(fileName);
            if (File.Exists(path))
                File.Delete(path);
            FileSystemManager.ReSync();
            return false;
        }
    }

    [HarmonyPatch(typeof(FileSystemUnityDefault), "Save")]
    public static class FileSystemUnityDefaultSavePatch
    {
        private static bool Prefix(string fileName, byte[] data)
        {
            APSaveProfileService.EnsureFolder();
            File.WriteAllBytes(APSaveProfileService.GetGameSavePath(fileName), data);
            FileSystemManager.ReSync();
            return false;
        }
    }

    [HarmonyPatch(typeof(FileSystemUnityDefault), "Load")]
    public static class FileSystemUnityDefaultLoadPatch
    {
        private static bool Prefix(string fileName, ref byte[] __result)
        {
            string path = APSaveProfileService.GetGameSavePath(fileName);
            __result = File.Exists(path) ? File.ReadAllBytes(path) : null;
            return false;
        }
    }
}

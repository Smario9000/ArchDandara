/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for active save detection and new-game AP state handling.
 * Why: The mod needs to know when a save is active so it can replay AP items and reopen checked locations.
 * Notes: Save hooks are the safest place to detect new game starts and queue AP item replay.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using Dandara.Save;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(GameSaveWidget), "Load")]
    public static class GameSaveWidgetLoadPatch
    {
        private static void Prefix(GameSaveWidget __instance)
        {
            if (__instance == null || string.IsNullOrEmpty(__instance.saveSlotName))
                return;

            try
            {
                if (!FileSystemManager.HasSave(__instance.saveSlotName))
                {
                    SaveSync.ResetForNewGame(__instance.saveSlotName);
                    APClient.RequestCurrentSaveResync();
                }
            }
            catch (System.Exception ex)
            {
                MLLog.Error("[Patch][SaveManager] New game AP resync failed: " + ex);
            }
        }
    }

    [HarmonyPatch(typeof(SaveManager), "SetCurrentSaveName")]
    public static class SaveManagerSetCurrentSaveNamePatch
    {
        private static string LastSaveName;

        private static void Postfix(string saveName)
        {
            SaveSync.SetGameSaveName(saveName);
            if (LastSaveName == saveName)
                return;

            LastSaveName = saveName;
            MLLog.Msg("[Patch][SaveManager] Active save: " + saveName);
        }
    }

    [HarmonyPatch(typeof(SaveManager), "Save")]
    public static class SaveManagerSavePatch
    {
        private static void Postfix()
        {
            SaveSync.Save();
        }
    }
}

/*
 * ArchDandara documentation
 * Purpose: Chooses and guards the AP-specific save folder for player and seed.
 * Why: Separate AP saves prevent cross-seed and cross-slot item or chest state corruption.
 * Notes: Connection guards prevent changing to a different AP slot while a gameplay save is active.
 */

using System.IO;
using ArchDandara.Config;
using Dandara.Save;

namespace ArchDandara.Game
{
    public static class APSaveProfileService
    {
        private static string ActiveSlotName;
        private static string ActiveSeedName;
        private static string ActiveFolder;

        public static bool HasActiveProfile
        {
            get { return !string.IsNullOrEmpty(ActiveFolder); }
        }

        public static void SetActiveProfile(string slotName, string seedName)
        {
            if (string.IsNullOrEmpty(slotName))
                slotName = "Player";
            if (string.IsNullOrEmpty(seedName))
                seedName = "NoSeed";

            string folder = Path.Combine(APConfig.SaveFolder,
                CleanFilePart("Dandara_" + slotName + "_" + seedName));

            if (ActiveFolder == folder)
                return;

            SaveSync.Save();
            ActiveSlotName = slotName;
            ActiveSeedName = seedName;
            ActiveFolder = folder;
            EnsureFolder();
            SaveSync.ReloadFromStorage();

            try
            {
                FileSystemManager.ReSync();
            }
            catch (System.Exception ex)
            {
                MLLog.Warning("[SaveProfile] Failed to resync save menu: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }

            MLLog.Msg("[SaveProfile] Active AP save folder: " + ActiveFolder);
        }

        public static bool CanConnectToSlot(string slotName, out string reason)
        {
            reason = null;
            if (string.IsNullOrEmpty(ActiveSlotName) || ActiveSlotName == slotName)
                return true;

            if (!IsInGame())
                return true;

            reason = "Cannot reconnect as '" + slotName + "' while this save is active as '" +
                     ActiveSlotName + "'. Return to the main menu before changing SlotName.";
            return false;
        }

        public static string GetStorageFolder()
        {
            if (string.IsNullOrEmpty(ActiveFolder))
            {
                return Path.Combine(APConfig.SaveFolder,
                    CleanFilePart("Dandara_" + APConfig.SlotName + "_NoSeed"));
            }

            return ActiveFolder;
        }

        public static string GetGameSavePath(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
                saveName = "default";

            return Path.Combine(GetStorageFolder(), CleanFilePart(saveName) + ".banana");
        }

        public static void EnsureFolder()
        {
            string folder = GetStorageFolder();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
        }

        private static bool IsInGame()
        {
            return !object.ReferenceEquals(GameAccess.Player, null);
        }

        private static string CleanFilePart(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "default";

            char[] invalid = Path.GetInvalidFileNameChars();
            string result = value.Trim();
            for (int i = 0; i < invalid.Length; i++)
                result = result.Replace(invalid[i], '_');

            result = result.Replace(' ', '_');
            return result.Length == 0 ? "default" : result;
        }
    }
}

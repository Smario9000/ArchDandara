/*
 * ArchDandara documentation
 * Purpose: Persists AP checked locations, received item counts, shop state, permits, and replay markers.
 * Why: AP state must survive restarts and new-game resets while still allowing server resync.
 * Notes: This is the local AP truth for the current save; server sync can import data but should not erase unrelated local state.
 */

using System.Collections.Generic;
using System.IO;
using ArchDandara.Config;

namespace ArchDandara.Game
{
    public static class SaveSync
    {
        private static readonly HashSet<long> CheckedLocations = new HashSet<long>();
        private static readonly HashSet<string> ShopPermits = new HashSet<string>();
        private static readonly Dictionary<string, int> ShopBoughtCounts = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> ReceivedItemCounts = new Dictionary<string, int>();
        private static string CurrentGameSaveName = "default";
        private static bool HasActiveGameSave;
        private static int ShopSaltSpent;
        private static bool AmmoHudBootstrapped;
        public static int ProcessedReceivedItemCount;
        public static bool NeedsServerResync;

        public static bool IsGameSaveActive
        {
            get { return HasActiveGameSave; }
        }

        public static void Initialize()
        {
            CheckedLocations.Clear();
            ShopPermits.Clear();
            ShopBoughtCounts.Clear();
            ReceivedItemCounts.Clear();
            HasActiveGameSave = false;
            ShopSaltSpent = 0;
            AmmoHudBootstrapped = false;
            ProcessedReceivedItemCount = 0;
            NeedsServerResync = false;
            Load();
        }

        public static void ReloadFromStorage()
        {
            CheckedLocations.Clear();
            ShopPermits.Clear();
            ShopBoughtCounts.Clear();
            ReceivedItemCounts.Clear();
            ShopSaltSpent = 0;
            AmmoHudBootstrapped = false;
            ProcessedReceivedItemCount = 0;
            NeedsServerResync = false;
            Load();
        }

        public static void SetGameSaveName(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
                saveName = "default";

            if (CurrentGameSaveName == saveName)
            {
                HasActiveGameSave = true;
                return;
            }

            Save();
            CurrentGameSaveName = saveName;
            HasActiveGameSave = true;
            CheckedLocations.Clear();
            ShopPermits.Clear();
            ShopBoughtCounts.Clear();
            ReceivedItemCounts.Clear();
            ShopSaltSpent = 0;
            AmmoHudBootstrapped = false;
            ProcessedReceivedItemCount = 0;
            NeedsServerResync = false;
            Load();
        }

        public static void ResetForNewGame(string saveName)
        {
            if (string.IsNullOrEmpty(saveName))
                saveName = "default";

            Save();
            // A fresh vanilla save should lose checked item replay progress, but shop purchases
            // represent AP locations already sent to the server and must not become buyable again.
            Dictionary<string, int> preservedShopBoughtCounts =
                new Dictionary<string, int>(ShopBoughtCounts);
            int preservedShopSaltSpent = ShopSaltSpent;
            bool preservedAmmoHudBootstrapped = AmmoHudBootstrapped;

            CurrentGameSaveName = saveName;
            HasActiveGameSave = true;
            CheckedLocations.Clear();
            ShopPermits.Clear();
            ShopBoughtCounts.Clear();
            ReceivedItemCounts.Clear();
            foreach (KeyValuePair<string, int> boughtCount in preservedShopBoughtCounts)
                ShopBoughtCounts[boughtCount.Key] = boughtCount.Value;

            HasActiveGameSave = false;
            ShopSaltSpent = preservedShopSaltSpent;
            AmmoHudBootstrapped = preservedAmmoHudBootstrapped;
            ProcessedReceivedItemCount = 0;
            // This flag tells APClient to replay server-side received items after the new save is
            // actually in-game, avoiding grants while the main menu managers are still active.
            NeedsServerResync = true;

            string savePath = GetSavePath();
            if (File.Exists(savePath))
                File.Delete(savePath);

            Save();
            MLLog.Msg("[SaveSync] New game detected. Cleared AP state for save: " + CurrentGameSaveName);
        }

        public static void SetServerResyncNeeded(bool needed)
        {
            NeedsServerResync = needed;
            Save();
        }

        public static bool HasCheckedLocation(long locationId)
        {
            return CheckedLocations.Contains(locationId);
        }

        public static void MarkCheckedLocation(long locationId)
        {
            CheckedLocations.Add(locationId);
        }

        public static bool HasShopPermit(string permitName)
        {
            return !string.IsNullOrEmpty(permitName) && ShopPermits.Contains(permitName);
        }

        public static void MarkShopPermit(string permitName)
        {
            if (!string.IsNullOrEmpty(permitName))
                ShopPermits.Add(permitName);
        }

        public static void MarkShopBought(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return;

            int count;
            ShopBoughtCounts.TryGetValue(categoryName, out count);
            ShopBoughtCounts[categoryName] = count + 1;
        }

        public static int GetShopBoughtCount(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return 0;

            int count;
            return ShopBoughtCounts.TryGetValue(categoryName, out count) ? count : 0;
        }

        public static int GetTotalShopBoughtCount()
        {
            int total = 0;
            foreach (KeyValuePair<string, int> boughtCount in ShopBoughtCounts)
            {
                if (boughtCount.Value > 0)
                    total += boughtCount.Value;
            }

            return total;
        }

        public static void AddShopSaltSpent(int amount)
        {
            if (amount <= 0)
                return;

            ShopSaltSpent += amount;
        }

        public static int GetShopSaltSpent()
        {
            return ShopSaltSpent;
        }

        public static void SetShopSaltSpentMinimum(int amount)
        {
            if (amount > ShopSaltSpent)
                ShopSaltSpent = amount;
        }

        public static bool HasAmmoHudBootstrap()
        {
            return AmmoHudBootstrapped;
        }

        public static void MarkAmmoHudBootstrap()
        {
            AmmoHudBootstrapped = true;
        }

        public static void ImportCheckedLocations(System.Collections.Generic.IEnumerable<long> locationIds)
        {
            if (locationIds == null)
                return;

            int added = 0;
            foreach (long locationId in locationIds)
            {
                if (CheckedLocations.Add(locationId))
                    added++;
            }

            if (added > 0)
            {
                MLLog.Msg("[SaveSync] Imported checked AP locations: " + added);
                Save();
            }
        }

        public static void MarkReceivedItemProcessed(int itemIndex, string itemName)
        {
            if (itemIndex > ProcessedReceivedItemCount)
            {
                // AP item indexes are one-way progress markers. Advancing only forward prevents
                // reconnects from double-counting already processed network items.
                ProcessedReceivedItemCount = itemIndex;
                AddReceivedItemCount(itemName);
            }
        }

        public static void MarkReceivedItemReplayed(int itemIndex)
        {
            if (itemIndex > ProcessedReceivedItemCount)
                ProcessedReceivedItemCount = itemIndex;
        }

        public static int GetReceivedItemCount(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return 0;

            int count;
            return ReceivedItemCounts.TryGetValue(itemName, out count) ? count : 0;
        }

        public static void ReplaceReceivedItemCounts(System.Collections.Generic.IEnumerable<string> itemNames)
        {
            ReceivedItemCounts.Clear();
            if (itemNames == null)
                return;

            foreach (string itemName in itemNames)
                AddReceivedItemCount(itemName);

            Save();
        }

        private static void AddReceivedItemCount(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return;

            int count;
            ReceivedItemCounts.TryGetValue(itemName, out count);
            ReceivedItemCounts[itemName] = count + 1;
        }

        public static void Load()
        {
            string savePath = GetSavePath();
            if (!File.Exists(savePath))
                return;

            string[] lines = File.ReadAllLines(savePath);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("items="))
                {
                    int itemCount;
                    if (int.TryParse(line.Substring("items=".Length), out itemCount))
                        ProcessedReceivedItemCount = itemCount;
                    continue;
                }

                if (line.StartsWith("needs_resync="))
                {
                    string value = line.Substring("needs_resync=".Length);
                    NeedsServerResync = value == "1" || value.ToLower() == "true";
                    continue;
                }

                if (line.StartsWith("shop_salt_spent="))
                {
                    int amount;
                    if (int.TryParse(line.Substring("shop_salt_spent=".Length), out amount))
                        ShopSaltSpent = amount < 0 ? 0 : amount;
                    continue;
                }

                if (line.StartsWith("ammo_hud_bootstrap="))
                {
                    string value = line.Substring("ammo_hud_bootstrap=".Length);
                    AmmoHudBootstrapped = value == "1" || value.ToLower() == "true";
                    continue;
                }

                if (line.StartsWith("permit="))
                {
                    string permitName = line.Substring("permit=".Length);
                    if (!string.IsNullOrEmpty(permitName))
                        ShopPermits.Add(permitName);
                    continue;
                }

                if (line.StartsWith("item="))
                {
                    // Stored item counts power AP-only logic such as duplicate movement conversion,
                    // shop bars, and damage upgrade totals.
                    string value = line.Substring("item=".Length);
                    int marker = value.LastIndexOf('|');
                    if (marker > 0 && marker < value.Length - 1)
                    {
                        string itemName = value.Substring(0, marker);
                        int count;
                        if (int.TryParse(value.Substring(marker + 1), out count))
                            ReceivedItemCounts[itemName] = count;
                    }

                    continue;
                }

                if (line.StartsWith("shopbuy="))
                {
                    // Bought counts are category-scoped because all shop categories share one price
                    // table but have different AP location ranges and maximum counts.
                    string value = line.Substring("shopbuy=".Length);
                    int marker = value.LastIndexOf('|');
                    if (marker > 0 && marker < value.Length - 1)
                    {
                        string categoryName = value.Substring(0, marker);
                        int count;
                        if (int.TryParse(value.Substring(marker + 1), out count))
                            ShopBoughtCounts[categoryName] = count;
                    }

                    continue;
                }

                long locationId;
                if (long.TryParse(line, out locationId))
                    CheckedLocations.Add(locationId);
            }

            MLLog.Msg("[SaveSync] Loaded checked locations: " + CheckedLocations.Count +
                            ", received items: " + ProcessedReceivedItemCount +
                            ", shop permits: " + ShopPermits.Count +
                            ", shop bought categories: " + ShopBoughtCounts.Count +
                            ", tracked item counts: " + ReceivedItemCounts.Count);
        }

        public static void Save()
        {
            string storageFolder = APSaveProfileService.GetStorageFolder();
            if (!Directory.Exists(storageFolder))
                Directory.CreateDirectory(storageFolder);

            string[] lines = new string[CheckedLocations.Count + ShopPermits.Count + ShopBoughtCounts.Count +
                                        ReceivedItemCounts.Count + 5];
            int index = 0;
            lines[index] = "# ArchDandara AP state";
            index++;
            lines[index] = "items=" + ProcessedReceivedItemCount;
            index++;
            lines[index] = "needs_resync=" + (NeedsServerResync ? "1" : "0");
            index++;
            lines[index] = "shop_salt_spent=" + ShopSaltSpent;
            index++;
            lines[index] = "ammo_hud_bootstrap=" + (AmmoHudBootstrapped ? "1" : "0");
            index++;
            foreach (long locationId in CheckedLocations)
            {
                lines[index] = locationId.ToString();
                index++;
            }

            foreach (string permitName in ShopPermits)
            {
                lines[index] = "permit=" + permitName;
                index++;
            }

            foreach (KeyValuePair<string, int> boughtCount in ShopBoughtCounts)
            {
                lines[index] = "shopbuy=" + boughtCount.Key + "|" + boughtCount.Value;
                index++;
            }

            foreach (KeyValuePair<string, int> itemCount in ReceivedItemCounts)
            {
                // This file is intentionally human-readable so debugging save/AP mismatches does not
                // require a binary save editor.
                lines[index] = "item=" + itemCount.Key + "|" + itemCount.Value;
                index++;
            }

            File.WriteAllLines(GetSavePath(), lines);
        }

        private static string GetSavePath()
        {
            return Path.Combine(APSaveProfileService.GetStorageFolder(),
                "ap_state_" + CleanFilePart(APConfig.SlotName) + "_" + CleanFilePart(CurrentGameSaveName) + ".txt");
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

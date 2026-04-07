// DataBase.cs
// Handles all file output for scanned room data and live log data.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using ArchDandara.Debugging;

namespace ArchDandara.Database
{
    public static class DataManager
    {
        // Main output folder.
        private static string _folder;
        
        // Paths to the different output files.
        private static string _activityFile;
        private static string _doorsFile;
        private static string _checksFile;
        private static string _chestsFile;
        private static string _npcsFile;
        private static string _soulsFile;
        private static string _storyEventsFile;
        private static string _shopUpgradesFile;

        // Per-room signatures used to detect whether room data changed.
        private static Dictionary<string, string> _roomDoorSignatures = new Dictionary<string, string>();
        private static Dictionary<string, string> _roomChestSignatures = new Dictionary<string, string>();
        // ReSharper disable once InconsistentNaming
        private static Dictionary<string, string> _roomNPCSignatures = new Dictionary<string, string>();
        private static Dictionary<string, string> _roomSoulSignatures = new Dictionary<string, string>();
        private static Dictionary<string, string> _roomStorySignatures = new Dictionary<string, string>();
        private static Dictionary<string, string> _roomShopSignatures = new Dictionary<string, string>();

        // Small recent-log cache used to stop repeated spam.
        private static readonly object _recentLogLock = new object();
        private static Dictionary<string, DateTime> _recentLogTimes = new Dictionary<string, DateTime>();
        
        // Tracks whether each file has already been loaded into memory.
        private static bool _doorFileLoaded;
        private static bool _chestFileLoaded;
        private static bool _npcFileLoaded;
        private static bool _soulFileLoaded;
        private static bool _storyFileLoaded;
        private static bool _shopFileLoaded;

        // In-memory copy of each file's room blocks.
        private static Dictionary<string, string> _doorRoomBlocks = new Dictionary<string, string>();
        private static Dictionary<string, string> _chestRoomBlocks = new Dictionary<string, string>();
        private static Dictionary<string, string> _npcRoomBlocks = new Dictionary<string, string>();
        private static Dictionary<string, string> _soulRoomBlocks = new Dictionary<string, string>();
        private static Dictionary<string, string> _storyRoomBlocks = new Dictionary<string, string>();
        private static Dictionary<string, string> _shopRoomBlocks = new Dictionary<string, string>();

        // Sets up the /doc folder and all output file paths.
        public static void Init()
        {
            _folder = Path.Combine(MelonEnvironment.GameRootDirectory, "doc");

            if (!Directory.Exists(_folder))
            {
                Directory.CreateDirectory(_folder);
                MelonLogger.Msg("[DataManager] Created /doc folder");
            }

            _activityFile = Path.Combine(_folder, "activity_log.txt");
            _doorsFile = Path.Combine(_folder, "doors.txt");
            _checksFile = Path.Combine(_folder, "checks_log.txt");
            _chestsFile = Path.Combine(_folder, "chests.txt");
            _npcsFile = Path.Combine(_folder, "npcs.txt");
            _soulsFile = Path.Combine(_folder, "souls.txt");
            _storyEventsFile = Path.Combine(_folder, "storyevents.txt");
            _shopUpgradesFile = Path.Combine(_folder, "shopupgrades.txt");
        }

        // Saves door data for one room.
        public static void SaveRoom(string scene, List<Door> doors)
        {
            if (doors == null || doors.Count == 0)
                return;

            EnsureFileLoaded(_doorsFile, ref _doorFileLoaded, _doorRoomBlocks);

            string signature = BuildDoorSignature(doors);
            string block = BuildDoorBlock(scene, doors);

            if (_roomDoorSignatures.ContainsKey(scene) && _roomDoorSignatures[scene] == signature)
            {
                DebugLogger.Log("Door scan skipped: " + scene);
                return;
            }

            _roomDoorSignatures[scene] = signature;

            if (_doorRoomBlocks.TryGetValue(scene, out string existingBlock) && existingBlock == block)
            {
                DebugLogger.Log("Door block already current: " + scene);
                return;
            }

            _doorRoomBlocks[scene] = block;
            RewriteFile(_doorsFile, _doorRoomBlocks);
        }

        // Saves chest data for one room.
        public static void SaveRoomChests(string scene, List<PowerupInteractable> chests)
        {
            SaveMonoRoom(
                scene,
                chests,
                _roomChestSignatures,
                ref _chestFileLoaded,
                _chestRoomBlocks,
                _chestsFile,
                "Chest");
        }

        // Saves NPC data for one room.
        public static void SaveRoomNPCs(string scene, List<DialogueInteractable> npcs)
        {
            SaveMonoRoom(
                scene,
                npcs,
                _roomNPCSignatures,
                ref _npcFileLoaded,
                _npcRoomBlocks,
                _npcsFile,
                "NPC");
        }

        // Saves soul data for one room.
        public static void SaveRoomSouls(string scene, List<GameObject> souls)
        {
            if (souls == null)
                return;

            EnsureFileLoaded(_soulsFile, ref _soulFileLoaded, _soulRoomBlocks);

            string signature = BuildSoulSignature(souls);
            string block = BuildSoulBlock(scene, souls);

            if (_roomSoulSignatures.ContainsKey(scene) && _roomSoulSignatures[scene] == signature)
            {
                DebugLogger.Log("Soul scan skipped: " + scene);
                return;
            }

            _roomSoulSignatures[scene] = signature;

            if (_soulRoomBlocks.TryGetValue(scene, out string existingBlock) && existingBlock == block)
            {
                DebugLogger.Log("Soul block already current: " + scene);
                return;
            }

            _soulRoomBlocks[scene] = block;
            RewriteFile(_soulsFile, _soulRoomBlocks);
        }

        // Saves story-event object data for one room.
        public static void SaveRoomStoryEvents(string scene, List<GameObject> eventsList)
        {
            if (eventsList == null)
                return;

            EnsureFileLoaded(_storyEventsFile, ref _storyFileLoaded, _storyRoomBlocks);

            string signature = BuildGameObjectSignature(eventsList);
            string block = BuildGameObjectBlock(scene, eventsList, "Event");

            if (_roomStorySignatures.ContainsKey(scene) && _roomStorySignatures[scene] == signature)
            {
                DebugLogger.Log("Event scan skipped: " + scene);
                return;
            }

            _roomStorySignatures[scene] = signature;

            if (_storyRoomBlocks.TryGetValue(scene, out string existingBlock) && existingBlock == block)
            {
                DebugLogger.Log("Event block already current: " + scene);
                return;
            }

            _storyRoomBlocks[scene] = block;
            RewriteFile(_storyEventsFile, _storyRoomBlocks);
        }

        // Saves shop-upgrade proxy data for one room.
        public static void SaveRoomShopUpgrades(string scene, List<PowerupManagerProxy> upgrades)
        {
            if (upgrades == null)
                return;

            EnsureFileLoaded(_shopUpgradesFile, ref _shopFileLoaded, _shopRoomBlocks);

            string signature = BuildShopUpgradeSignature(upgrades);
            string block = BuildShopUpgradeBlock(scene, upgrades);

            if (_roomShopSignatures.ContainsKey(scene) && _roomShopSignatures[scene] == signature)
            {
                DebugLogger.Log("Upgrade scan skipped: " + scene);
                return;
            }

            _roomShopSignatures[scene] = signature;

            if (_shopRoomBlocks.TryGetValue(scene, out string existingBlock) && existingBlock == block)
            {
                DebugLogger.Log("Upgrade block already current: " + scene);
                return;
            }

            _shopRoomBlocks[scene] = block;
            RewriteFile(_shopUpgradesFile, _shopRoomBlocks);
        }

        // Shared helper for MonoBehaviour-based room data.
        private static void SaveMonoRoom<T>(
            string scene,
            List<T> list,
            Dictionary<string, string> signatures,
            ref bool fileLoaded,
            Dictionary<string, string> roomBlocks,
            string filePath,
            string label) where T : MonoBehaviour
        {
            if (list == null)
                return;

            EnsureFileLoaded(filePath, ref fileLoaded, roomBlocks);

            string signature = BuildMonoSignature(list);
            string block = BuildMonoBlock(scene, list, label);

            if (signatures.ContainsKey(scene) && signatures[scene] == signature)
            {
                DebugLogger.Log(label + " scan skipped: " + scene);
                return;
            }

            signatures[scene] = signature;

            if (roomBlocks.TryGetValue(scene, out string existingBlock) && existingBlock == block)
            {
                DebugLogger.Log(label + " block already current: " + scene);
                return;
            }

            roomBlocks[scene] = block;
            RewriteFile(filePath, roomBlocks);
        }

        // Builds a compact signature for one room's doors.
        private static string BuildDoorSignature(List<Door> doors)
        {
            List<string> rows = new List<string>();

            foreach (Door door in doors)
            {
                if (door == null)
                    continue;

                string doorName = string.IsNullOrEmpty(door.name) ? "Door" : door.name;
                string destination = door._otherSideScene ?? string.Empty;
                Vector3 pos = door.transform.position;
                float rotZ = door.transform.eulerAngles.z;

                rows.Add(
                    doorName + "|" +
                    destination + "|" +
                    Round3(pos.x) + "|" +
                    Round3(pos.y) + "|" +
                    Round3(rotZ));
            }

            rows.Sort();
            return JoinRows(rows);
        }

        // Builds the visible text block for one room's doors.
        private static string BuildDoorBlock(string scene, List<Door> doors)
        {
            List<string> rows = new List<string>();

            foreach (Door door in doors)
            {
                if (door == null)
                    continue;

                string doorName = string.IsNullOrEmpty(door.name) ? "Door" : door.name;
                string destination = door._otherSideScene ?? string.Empty;
                Vector3 pos = door.transform.position;
                float rotZ = door.transform.eulerAngles.z;

                rows.Add(
                    "Door -> " +
                    doorName + " -> " +
                    destination + " -> (" +
                    Round3(pos.x) + "," +
                    Round3(pos.y) + "," +
                    Round3(rotZ) + ")");
            }

            rows.Sort();

            string text = "The Room: " + scene + "\n";

            if (rows.Count == 0)
            {
                text += "Door -> NONE\n";
            }
            else
            {
                foreach (string row in rows)
                {
                    text += row + "\n";
                }
            }

            text += "--- END ROOM ---\n\n";
            return text;
        }

        // Builds a compact signature for generic MonoBehaviour room data.
        private static string BuildMonoSignature<T>(List<T> list) where T : MonoBehaviour
        {
            List<string> rows = new List<string>();

            foreach (T obj in list)
            {
                if (obj == null)
                    continue;

                Vector3 pos = obj.transform.position;

                rows.Add(
                    (string.IsNullOrEmpty(obj.name) ? "Object" : obj.name) + "|" +
                    obj.GetType().Name + "|" +
                    Round3(pos.x) + "|" +
                    Round3(pos.y) + "|" +
                    Round3(pos.z));
            }

            rows.Sort();
            return JoinRows(rows);
        }

        // Builds the visible text block for generic MonoBehaviour room data.
        private static string BuildMonoBlock<T>(string scene, List<T> list, string label) where T : MonoBehaviour
        {
            List<string> rows = new List<string>();

            foreach (T obj in list)
            {
                if (obj == null)
                    continue;

                Vector3 pos = obj.transform.position;
                string objName = string.IsNullOrEmpty(obj.name) ? label : obj.name;
                string extra = obj.GetType().Name;

                rows.Add(
                    label + " -> " +
                    objName + " -> " +
                    extra + " -> (" +
                    Round3(pos.x) + "," +
                    Round3(pos.y) + "," +
                    Round3(pos.z) + ")");
            }

            rows.Sort();

            string text = "The Room: " + scene + "\n";

            if (rows.Count == 0)
            {
                text += label + " -> NONE\n";
            }
            else
            {
                foreach (string row in rows)
                {
                    text += row + "\n";
                }
            }

            text += "--- END ROOM ---\n\n";
            return text;
        }

        // Builds a compact signature for shop-upgrade proxies.
        private static string BuildShopUpgradeSignature(List<PowerupManagerProxy> upgrades)
        {
            List<string> rows = new List<string>();

            foreach (PowerupManagerProxy proxy in upgrades)
            {
                if (proxy == null || proxy.gameObject == null)
                    continue;

                Vector3 pos = proxy.transform.position;

                rows.Add(
                    (string.IsNullOrEmpty(proxy.gameObject.name) ? "Upgrade" : proxy.gameObject.name) + "|" +
                    proxy.toUnlock + "|" +
                    (proxy.uniqueID ?? string.Empty) + "|" +
                    Round3(pos.x) + "|" +
                    Round3(pos.y) + "|" +
                    Round3(pos.z));
            }

            rows.Sort();
            return JoinRows(rows);
        }

        // Builds the visible text block for shop-upgrade proxies.
        private static string BuildShopUpgradeBlock(string scene, List<PowerupManagerProxy> upgrades)
        {
            List<string> rows = new List<string>();

            foreach (PowerupManagerProxy proxy in upgrades)
            {
                if (proxy == null || proxy.gameObject == null)
                    continue;

                Vector3 pos = proxy.transform.position;

                rows.Add(
                    "Upgrade -> " +
                    (string.IsNullOrEmpty(proxy.gameObject.name) ? "Upgrade" : proxy.gameObject.name) + " -> " +
                    proxy.toUnlock + " -> " +
                    (proxy.uniqueID ?? string.Empty) + " -> (" +
                    Round3(pos.x) + "," +
                    Round3(pos.y) + "," +
                    Round3(pos.z) + ")");
            }

            rows.Sort();

            string text = "The Room: " + scene + "\n";

            if (rows.Count == 0)
            {
                text += "Upgrade -> NONE\n";
            }
            else
            {
                foreach (string row in rows)
                {
                    text += row + "\n";
                }
            }

            text += "--- END ROOM ---\n\n";
            return text;
        }
        
        // Builds a compact signature for soul objects.
        private static string BuildSoulSignature(List<GameObject> souls)
        {
            List<string> rows = new List<string>();

            foreach (GameObject soul in souls)
            {
                if (soul == null)
                    continue;

                Vector3 pos = soul.transform.position;
                Vector3 rot = soul.transform.eulerAngles;

                string soulName = Room_Area.SoulScanner.GuessSoulName(soul);
                string rewardName = Room_Area.SoulScanner.GuessSoulReward(soul);

                rows.Add(
                    soulName + "|" +
                    rewardName + "|" +
                    Round3(pos.x) + "|" +
                    Round3(pos.y) + "|" +
                    Round3(pos.z) + "|" +
                    Round3(rot.x) + "|" +
                    Round3(rot.y) + "|" +
                    Round3(rot.z));
            }

            rows.Sort();
            return JoinRows(rows);
        }

        // Builds the visible text block for soul objects.
        private static string BuildSoulBlock(string scene, List<GameObject> souls)
        {
            List<string> rows = new List<string>();

            foreach (GameObject soul in souls)
            {
                if (soul == null)
                    continue;

                Vector3 pos = soul.transform.position;
                Vector3 rot = soul.transform.eulerAngles;

                string soulName = Room_Area.SoulScanner.GuessSoulName(soul);
                string rewardName = Room_Area.SoulScanner.GuessSoulReward(soul);

                string row =
                    "Soul -> " +
                    soulName + " -> " +
                    rewardName + " -> (" +
                    Round3(pos.x) + "," +
                    Round3(pos.y) + "," +
                    Round3(pos.z) + ")";

                if (Room_Area.SoulScanner.ShouldShowRotation(rot))
                {
                    row += " Rot(" +
                           Round3(rot.x) + "," +
                           Round3(rot.y) + "," +
                           Round3(rot.z) + ")";
                }

                rows.Add(row);
            }

            rows.Sort();

            string text = "The Room: " + scene + "\n";

            if (rows.Count == 0)
            {
                text += "Soul -> NONE\n";
            }
            else
            {
                foreach (string row in rows)
                {
                    text += row + "\n";
                }
            }

            text += "--- END ROOM ---\n\n";
            return text;
        }

        // Builds a compact signature for generic GameObject room data.
        private static string BuildGameObjectSignature(List<GameObject> list)
        {
            List<string> rows = new List<string>();

            foreach (GameObject obj in list)
            {
                if (obj == null)
                    continue;

                Vector3 pos = obj.transform.position;

                rows.Add(
                    (string.IsNullOrEmpty(obj.name) ? "Object" : obj.name) + "|" +
                    Round3(pos.x) + "|" +
                    Round3(pos.y) + "|" +
                    Round3(pos.z));
            }

            rows.Sort();
            return JoinRows(rows);
        }

        // Builds the visible text block for generic GameObject room data.
        private static string BuildGameObjectBlock(string scene, List<GameObject> list, string label)
        {
            List<string> rows = new List<string>();

            foreach (GameObject obj in list)
            {
                if (obj == null)
                    continue;

                Vector3 pos = obj.transform.position;

                rows.Add(
                    label + " -> " +
                    (string.IsNullOrEmpty(obj.name) ? label : obj.name) + " -> (" +
                    Round3(pos.x) + "," +
                    Round3(pos.y) + "," +
                    Round3(pos.z) + ")");
            }

            rows.Sort();

            string text = "The Room: " + scene + "\n";

            if (rows.Count == 0)
            {
                text += label + " -> NONE\n";
            }
            else
            {
                foreach (string row in rows)
                {
                    text += row + "\n";
                }
            }

            text += "--- END ROOM ---\n\n";
            return text;
        }
        
        // Looks up the room name shown on the in-game map.
        public static string GetRoomNameForScene(string scene)
        {
            try
            {
                if (PersistentSingleton<MapManager>.instance == null ||
                    PersistentSingleton<MapManager>.instance.CurrentMap == null ||
                    string.IsNullOrEmpty(scene))
                {
                    return string.Empty;
                }

                MapRoom room = PersistentSingleton<MapManager>.instance.CurrentMap.GetRelatedMapRoom(scene);
                if (room == null)
                    return string.Empty;

                string roomName = room.GetRoomName();
                return roomName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Looks up the area name shown on the in-game map.
        public static string GetAreaNameForScene(string scene)
        {
            try
            {
                if (PersistentSingleton<MapManager>.instance == null ||
                    PersistentSingleton<MapManager>.instance.CurrentMap == null ||
                    string.IsNullOrEmpty(scene))
                {
                    return string.Empty;
                }

                MapRoom room = PersistentSingleton<MapManager>.instance.CurrentMap.GetRelatedMapRoom(scene);
                if (room == null)
                    return string.Empty;

                string areaName = room.GetAreaName();
                return areaName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Builds a short room/area text string for logs.
        public static string GetRoomMetaText(string scene)
        {
            string area = GetAreaNameForScene(scene);
            string room = GetRoomNameForScene(scene);

            if (!string.IsNullOrEmpty(area) && !string.IsNullOrEmpty(room) && area != room)
                return "Area=" + area + " Room=" + room;

            if (!string.IsNullOrEmpty(room))
                return "Room=" + room;

            if (!string.IsNullOrEmpty(area))
                return "Area=" + area;

            return string.Empty;
        }
        
        // Writes one activity/event log line.
        public static void LogActivity(string category, string scene, string source, string detail)
        {
            try
            {
                int windowMs = 750;

                // Money gain is allowed to spam more freely.
                if (category == "MoneyGain")
                    windowMs = 0;

                string dedupeKey = "A|" + category + "|" + scene + "|" + source + "|" + detail;
                if (ShouldSkipRecentLog(dedupeKey, windowMs))
                    return;

                string line =
                    "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " +
                    category + " | " +
                    scene + " | " +
                    source + " | " +
                    detail + "\n";

                File.AppendAllText(_activityFile, line);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DataManager] Failed writing activity log: " + ex.Message);
            }
        }

        // Writes one check/reward log line.
        public static void LogCheck(string checkType, string scene, string source, string reward, string extra)
        {
            try
            {
                int windowMs = 750;

                string dedupeKey = "C|" + checkType + "|" + scene + "|" + source + "|" + reward + "|" + extra;
                if (ShouldSkipRecentLog(dedupeKey, windowMs))
                    return;

                string line =
                    "[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " +
                    checkType + " | " +
                    scene + " | " +
                    source + " | " +
                    reward + " | " +
                    extra + "\n";

                File.AppendAllText(_checksFile, line);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DataManager] Failed writing checks log: " + ex.Message);
            }
        }
        
        // Blocks duplicate live-log lines for a short time window.
        private static bool ShouldSkipRecentLog(string key, int windowMs)
        {
            if (windowMs <= 0)
                return false;

            DateTime now = DateTime.UtcNow;

            Monitor.Enter(_recentLogLock);
            try
            {
                DateTime lastTime;
                if (_recentLogTimes.TryGetValue(key, out lastTime))
                {
                    double elapsedMs = (now - lastTime).TotalMilliseconds;
                    if (elapsedMs >= 0 && elapsedMs <= windowMs)
                        return true;
                }

                _recentLogTimes[key] = now;

                // Removes old entries from the recent-log cache.
                List<string> stale = null;
                foreach (KeyValuePair<string, DateTime> pair in _recentLogTimes)
                {
                    if ((now - pair.Value).TotalMinutes > 10)
                    {
                        if (stale == null)
                            stale = new List<string>();

                        stale.Add(pair.Key);
                    }
                }

                if (stale != null)
                {
                    for (int i = 0; i < stale.Count; i++)
                        _recentLogTimes.Remove(stale[i]);
                }
            }
            finally
            {
                Monitor.Exit(_recentLogLock);
            }

            return false;
        }
        
        // Loads an existing room file into memory the first time it is needed.
        private static void EnsureFileLoaded(string filePath, ref bool loaded, Dictionary<string, string> roomBlocks)
        {
            if (loaded)
                return;

            loaded = true;
            roomBlocks.Clear();

            if (!File.Exists(filePath))
                return;

            try
            {
                string allText = File.ReadAllText(filePath);
                string[] split = allText.Split(new[] { "--- END ROOM ---" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string raw in split)
                {
                    string block = raw.Trim();
                    if (block.Length == 0)
                        continue;

                    string[] lines = block.Split('\n');
                    if (lines.Length == 0)
                        continue;

                    string header = lines[0].Trim();
                    const string prefix = "The Room: ";

                    if (!header.StartsWith(prefix))
                        continue;

                    string scene = header.Substring(prefix.Length).Trim();
                    roomBlocks[scene] = block + "\n--- END ROOM ---\n\n";
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DataManager] Failed reading " + filePath + ": " + ex.Message);
            }
        }

        // Rewrites a room-data file from the in-memory room block dictionary.
        private static void RewriteFile(string filePath, Dictionary<string, string> roomBlocks)
        {
            try
            {
                List<string> keys = new List<string>(roomBlocks.Keys);
                keys.Sort();

                string allText = string.Empty;

                foreach (string key in keys)
                {
                    allText += roomBlocks[key];
                }

                File.WriteAllText(filePath, allText);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DataManager] Failed writing " + filePath + ": " + ex.Message);
            }
        }

        // Joins signature rows into one compact string.
        private static string JoinRows(List<string> rows)
        {
            string text = string.Empty;

            for (int i = 0; i < rows.Count; i++)
            {
                if (i > 0)
                    text += ";";

                text += rows[i];
            }

            return text;
        }

        // Rounds a float to 3 decimals for clean text output.
        private static string Round3(float value)
        {
            return ((float)Math.Round(value, 3)).ToString(CultureInfo.InvariantCulture);
        }
    }
}
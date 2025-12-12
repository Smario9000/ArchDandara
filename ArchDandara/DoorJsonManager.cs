//DoorJsonManager.cs

using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace ArchDandara
{
    public class DoorJsonManager
    {
        private string _jsonDirectory;
        private string _jsonFile;
        private DoorDatabase _database;
        //This is the constructor 
        // ============================================================================================
        // CONSTRUCTOR
        // ============================================================================================
        public DoorJsonManager()
        {
            MelonLogger.Msg("[DoorJsonManager] Initializing...");

            // Initialize new database using the NEW grouped system:
            _database = new DoorDatabase()
            {
                scenes = new List<SceneDoorGroup>()
            };

            _jsonDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "Dandara_Doors");
            MelonLogger.Msg("[DoorJsonManager] Directory Path = " + _jsonDirectory);

            if (!Directory.Exists(_jsonDirectory))
            {
                MelonLogger.Msg("[DoorJsonManager] Creating Directory...");
                Directory.CreateDirectory(_jsonDirectory);
            }

            _jsonFile = Path.Combine(_jsonDirectory, "door_database.json");
            MelonLogger.Msg("[DoorJsonManager] JSON File = " + _jsonFile);

            Load();
        }

        // ============================================================================================
        // LOAD — JSON → DoorDatabase using Newtonsoft
        // ============================================================================================
        private void Load()
        {
            if (!File.Exists(_jsonFile))
            {
                MelonLogger.Msg("[DoorJsonManager] No JSON found — creating new DB.");
                _database = new DoorDatabase();
                Save();
                return;
            }

            try
            {
                string json = File.ReadAllText(_jsonFile);
                // NEWTONSOFT VERSION
                var loaded = JsonConvert.DeserializeObject<DoorDatabase>(json);
                // SAFETY CHECKS
                if (loaded == null)
                {
                    MelonLogger.Warning("[DoorJsonManager] JSON was NULL — rebuilding database.");
                    _database = new DoorDatabase();
                    Save();
                    return;
                }

                _database = loaded;
                MelonLogger.Msg("[DoorJsonManager] Loaded door database.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Loading: " + e);
                _database = new DoorDatabase();
            }
        }

        // ============================================================================================
        // SAVE — DoorDatabase → JSON using Newtonsoft
        // ============================================================================================
        private void Save()
        {
            try
            {
                MelonLogger.Msg($"[DoorJsonManager] This is _database: {_database}");

                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                File.WriteAllText(_jsonFile, json);
                MelonLogger.Msg("[DoorJsonManager] Saved grouped database.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Saving: " + e);
            }
        }

        // ============================================================================================
        // UPDATE/INSERT DOOR
        // ============================================================================================
        public void AddOrUpdateDoor(DoorRecord entry)
        {
            // 1 — Find a group for this scene
            var group = _database.scenes.Find(s => s.sceneName == entry.sceneName);

            // 2 — Create new group if missing
            if (group == null)
            {
                group = new SceneDoorGroup
                {
                    sceneName = entry.sceneName,
                    doors = new List<DoorRecord>()
                };

                _database.scenes.Add(group);
            }

            // 3 — Remove any old door with same name
            group.doors.RemoveAll(d => d.doorName == entry.doorName);

            // 4 — Add new door object
            group.doors.Add(entry);

            // 5 — Save grouped structure
            Save();
        }
        public void PrintJsonToLog()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                MelonLogger.Msg("[DoorJsonManager] FINAL JSON OUTPUT:\n" + json);
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Printing JSON: " + e);
            }
        }
    }
}
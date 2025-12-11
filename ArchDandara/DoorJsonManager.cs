using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;
using UnityEngine;

namespace ArchDandara
{
    public class DoorJsonManager
    {
        private string _jsonDirectory;
        private string _jsonFile;
        private DoorDatabase _database;
        //This is the constructor 
        public DoorJsonManager()
        {
            MelonLogger.Msg("[DoorJsonManager] Initializing...");
            //This is to never have null
            _database = new DoorDatabase()
            {
                doors = new List<DoorRecord>()
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

        private void Load()
        {
            if (!File.Exists(_jsonFile))
            {
                MelonLogger.Msg("[DoorJsonManager] No JSON found — creating new DB.");
                _database = new DoorDatabase { doors = new List<DoorRecord>() };
                Save();
                return;
            }

            try
            {
                string json = File.ReadAllText(_jsonFile);

                // Unity JSON REQUIREMENT: wrapper object MUST NOT be null
                //var loaded = JsonUtility.FromJson<DoorDatabase>(json);
                _database = JsonUtility.FromJson<DoorDatabase>(json);
                if (_database == null || _database.doors == null)
                {
                    MelonLogger.Warning("[DoorJsonManager] JSON was empty — rebuilding.");
                    _database = new DoorDatabase { doors = new List<DoorRecord>() };
                }
                MelonLogger.Msg("[DoorJsonManager] Loaded door database.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Loading: " + e);
                _database = new DoorDatabase { doors = new List<DoorRecord>() };
            }
        }

        public void Save()
        {
            try
            {
                MelonLogger.Msg($"[DoorJsonManager] This is _database: {_database}");

                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                MelonLogger.Msg( $"[DoorJsonManager] Writing JSON {json}");
                File.WriteAllText(_jsonFile, json);
                MelonLogger.Msg("[DoorJsonManager] Saved database.");
            }
            catch (Exception e)
            {
                MelonLogger.Error("[DoorJsonManager] ERROR Saving: " + e);
            }
        }

        public void AddOrUpdateDoor(DoorRecord entry)
        {
            // Remove old entries
            _database.doors.RemoveAll(d =>
                d.sceneName == entry.sceneName &&
                d.doorName == entry.doorName
            );

            _database.doors.Add(entry);

            Save();
        }
    }
}
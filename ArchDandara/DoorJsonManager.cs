//DoorJsonManager.cs

using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace ArchDandara
{
    public class DoorJsonManager :  MelonLogger
    {
        private static string _jsonFile;
        private static DoorDatabase _database;
        //This is the constructor 
        public static void Print(string msg, int level = 1)
        {
            if (!ArchDandaraConfig.LogDoorJsonManager)
                return; 
            
            switch (level)
            {
                case 1:
                    MelonLogger.Msg("[DoorJsonManager] " + msg);
                    break;

                case 2:
                    MelonLogger.Warning("[DoorJsonManager] " + msg);
                    break;

                case 3:
                    MelonLogger.Error("[DoorJsonManager] " + msg);
                    break;
            }
        }
        // ============================================================================================
        // CONSTRUCTOR
        // ============================================================================================
        public static void Init()
        {
            {
                Print("Initializing...");

                // Initialize a new database using the NEW grouped system:
                _database = new DoorDatabase()
                {
                    Scenes = new List<SceneDoorGroup>()
                };

                var jsonDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "Dandara_Doors");
                Print("Directory Path = " + jsonDirectory);

                if (!Directory.Exists(jsonDirectory))
                {
                    Print("Creating Directory...");
                    Directory.CreateDirectory(jsonDirectory);
                }

                _jsonFile = Path.Combine(jsonDirectory, "door_database.json");
                Print("JSON File = " + _jsonFile);
                
                Load();
                RoomDoorScanner.Init();
            } 
        }

        // ============================================================================================
        // LOAD — JSON → DoorDatabase using Newtonsoft
        // ============================================================================================
        private static void Load()
        {
            if (!File.Exists(_jsonFile))
            {
                Print("No JSON found — creating new DB.", 2);
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
                    Print("JSON was NULL — rebuilding database.", 2);
                    _database = new DoorDatabase();
                    Save();
                    return;
                }

                _database = loaded;
                Print("Loaded door database.");
            }
            catch (Exception e)
            {
                Print("ERROR Loading: " + e, 3);
                _database = new DoorDatabase();
            }
        }

        // ============================================================================================
        // SAVE — DoorDatabase → JSON using Newtonsoft
        // ============================================================================================
        private static void Save()
        {
            try
            {
                Print("This is _database:" + _database);

                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                File.WriteAllText(_jsonFile, json);
                Print("Saved grouped database.");
            }
            catch (Exception e)
            {
                Print("ERROR Saving: " + e, 3);
            }
        }

        // ============================================================================================
        // UPDATE/INSERT DOOR
        // ============================================================================================
        public void AddOrUpdateDoor(DoorRecord entry)
        {
            // ❌ Do nothing if scanning disabled
            if (!ArchDandaraConfig.EnableRoomScanning)
                return;
            
            // 1 — Find a group for this scene
            var group = _database.Scenes.Find(s => s.SceneName == entry.SceneName);

            // 2 — Create a new group if missing
            if (group == null)
            {
                group = new SceneDoorGroup
                {
                    SceneName = entry.SceneName,
                    Doors = new List<DoorRecord>()
                };

                _database.Scenes.Add(group);
            }

            // 3 — Remove any old door with the same name
            group.Doors.RemoveAll(d => d.DoorName == entry.DoorName);

            // 4 — Add a new door object
            group.Doors.Add(entry);

            // 5 — Save grouped structure
            Save();
        }
        public void PrintJsonToLog()
        {
            try
            {
                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                Print("FINAL JSON OUTPUT:\n" + json);
            }
            catch (Exception e)
            {
                Print("ERROR Printing JSON: " + e, 3);
            }
        }
    }
}
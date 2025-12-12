//DoorJsonManager.cs

using System;
using System.Collections.Generic;
using System.IO;
using MelonLoader;
using MelonLoader.Utils;
using Newtonsoft.Json;

namespace ArchDandara
{
    // ====================================================================================================
    //  DoorJsonManager
    // ----------------------------------------------------------------------------------------------------
    //  This class manages ALL JSON interaction for the Dandara Archipelago mod.
    //  It is responsible for:
    //
    //    ✔ Creating door_database.json if it does not exist
    //    ✔ Loading all door groups/scenes from JSON using Newtonsoft.Json
    //    ✔ Adding or updating door entries when the RoomDoorScanner finds new doors
    //    ✔ Saving the database safely and formatting it in a readable way
    //    ✔ Printing logs depending on user config flags
    //
    //  The JSON layout looks like:
    //
    //      {
    //        "Scenes": [
    //          {
    //            "SceneName": "Some_Scene",
    //            "Doors": [
    //              {
    //                "DoorName": "DoorLeft",
    //                "OtherSideScene": "Hub",
    //                "PosX": 12.5,
    //                "PosY": -3.1,
    //                "PosZ": 0
    //              }
    //            ]
    //          }
    //        ]
    //      }
    //
    //  This allows *external editing* of door routes for Archipelago item randomization.
    // ====================================================================================================

    public class DoorJsonManager : MelonLogger
    {
        // Path to the JSON file storing door data
        private static string _jsonFile;

        // Cached version of the in-memory database structure
        private static DoorDatabase _database;

        // ====================================================================================================
        //  first time logging concept explained
        // ----------------------------------------------------------------------------------------------------
        //  The LogDoorJsonManager flag controls whether this system prints anything.
        //
        //  level = 1 → Info  
        //  level = 2 → Warning  
        //  level = 3 → Error  
        //
        //  NOTE: We DO NOT return strings here because MelonLogger expects void.
        // ====================================================================================================
        public static void Print(string msg, int level = 1)
        {
            if (!ArchDandaraConfig.LogDoorJsonManager)
                return; // Logging disabled per config

            switch (level)
            {
                case 1: MelonLogger.Msg("[DoorJsonManager] " + msg); break;
                case 2: MelonLogger.Warning("[DoorJsonManager] " + msg); break;
                case 3: MelonLogger.Error("[DoorJsonManager] " + msg); break;
            }
        }
        
        // ====================================================================================================
        //  Init() — Primary Initialization Entry Point
        // ----------------------------------------------------------------------------------------------------
        //  This method:
        //    1. Creates the directory where door JSON is stored
        //    2. Creates the JSON file path
        //    3. Creates an EMPTY DoorDatabase object
        //    4. Loads the JSON file if it exists (or creates one if not)
        //    5. Initializes the RoomDoorScanner AFTER the database is ready
        //
        //  IMPORTANT:
        //  DoorJsonManager *must* initialize BEFORE RoomDoorScanner so scanning can write to DB correctly.
        // ====================================================================================================
        public static void Init()
        {
            Print("Initializing...");

            // Create an empty DB structure (Scenes list)
            _database = new DoorDatabase()
            {
                Scenes = new List<SceneDoorGroup>()
            };

            // Folder containing the JSON file
            string jsonDirectory = Path.Combine(MelonEnvironment.UserDataDirectory, "Dandara_Doors");
            Print("Directory Path = " + jsonDirectory);

            if (!Directory.Exists(jsonDirectory))
            {
                Print("Creating Directory...");
                Directory.CreateDirectory(jsonDirectory);
            }

            // Full path to the JSON file
            _jsonFile = Path.Combine(jsonDirectory, "door_database.json");
            Print("JSON File = " + _jsonFile);

            // Load or create the database
            Load();

            // Scanner must start AFTER DB loads
            RoomDoorScanner.Init();
        }


        // ====================================================================================================
        //  Load() — Reads JSON file into DoorDatabase
        // ----------------------------------------------------------------------------------------------------
        //  Loads the door database from disk. If the file doesn’t exist, the database is created fresh
        //  and immediately saved.
        //
        //  Uses Newtonsoft.Json for predictable Unity/Mono compatibility.
        // ====================================================================================================
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
                var loaded = JsonConvert.DeserializeObject<DoorDatabase>(json);

                // Null check is important in case JSON was corrupted
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
                // If something is wrong (bad path, corrupted JSON), prevent crashes
                Print("ERROR Loading: " + e, 3);
                _database = new DoorDatabase();
            }
        }
        
        // ====================================================================================================
        //  Save() — Writes DoorDatabase to JSON file
        // ----------------------------------------------------------------------------------------------------
        //  Serializes the entire _database object and writes it to disk.
        //  Uses pretty-print formatting for readability.
        //
        //  In case of failure (bad permissions, missing folder), logs an error.
        // ====================================================================================================
        private static void Save()
        {
            try
            {
                Print("This is _database: " + _database);

                string json = JsonConvert.SerializeObject(_database, Formatting.Indented);
                File.WriteAllText(_jsonFile, json);

                Print("Saved grouped database.");
            }
            catch (Exception e)
            {
                Print("ERROR Saving: " + e, 3);
            }
        }
        
        // ====================================================================================================
        //  AddOrUpdateDoor()
        // ----------------------------------------------------------------------------------------------------
        //  This method receives a DoorRecord (from the RoomDoorScanner) and inserts it into the correct scene.
        //
        //    ✔ If the scene group does not exist → create a new SceneDoorGroup
        //    ✔ If the door already exists → replace it
        //    ✔ Always saves after updating
        //
        //  If EnableRoomScanning == false, this function does nothing.
        // ====================================================================================================
        public void AddOrUpdateDoor(DoorRecord entry)
        {
            if (!ArchDandaraConfig.EnableRoomScanning)
                return;

            // Try finding existing scene group
            var group = _database.Scenes.Find(s => s.SceneName == entry.SceneName);

            // If no group exists — create one
            if (group == null)
            {
                group = new SceneDoorGroup
                {
                    SceneName = entry.SceneName,
                    Doors = new List<DoorRecord>()
                };

                _database.Scenes.Add(group);
            }

            // Remove any old copy of this door
            group.Doors.RemoveAll(d => d.DoorName == entry.DoorName);

            // Add the new/updated door
            group.Doors.Add(entry);

            // Save changes
            Save();
        }
        
        // ====================================================================================================
        //  PrintJsonToLog()
        // ----------------------------------------------------------------------------------------------------
        //  Serializes the database and prints it to the MelonLoader console.
        //  Useful for debugging JSON output without opening files manually.
        // ====================================================================================================
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
using System.Collections.Generic;
using MelonLoader;

namespace ArchDandara
{
    public static class LocationManager
    {/*
        // Dictionary mapping Game ID -> Archipelago ID
        public static Dictionary<string, long> LocationMap = new Dictionary<string, long>()
        {
            // Example: You will need to find the REAL IDs by opening chests and checking the log!
            { "TutorialArea_Chest_1", 190001 }, 
            { "Garden_Chest_2", 190002 },
            // ... add all your checks here
        };

        public static void CheckLocation(string gameId)
        {
            if (LocationMap.TryGetValue(gameId, out long apId))
            {
                MelonLogger.Msg($"[Archipelago] Sending check for location: {gameId} (ID: {apId})");
                
                // Send to server!
                if (DandaraArchipelagoMod.Session != null && DandaraArchipelagoMod.Session.Socket.Connected)
                {
                    DandaraArchipelagoMod.Session.Locations.CompleteLocationChecksAsync(apId);
                }
            }
            else
            {
                MelonLogger.Warning($"[Archipelago] Unknown location ID found: {gameId}. Please add this to LocationManager.cs!");
            }
        }*/
    }
}
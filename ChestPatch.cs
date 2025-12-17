using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ArchDandara
{
    /*[HarmonyPatch(typeof(ChestInteractable), "Interact")]
    public class ChestPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(ChestInteractable __instance)
        {
            // Helper method to access private/protected 'IsUsed' method using Reflection if needed.
            // Assuming IsUsed() is public based on your snippet "this.FeedbackInteract(this.IsUsed())"
            // If IsUsed is protected, we might need a different approach, but let's try direct access first 
            // or use the Traverse helper if it fails to compile.
            
            bool alreadyOpened = Traverse.Create(__instance).Method("IsUsed").GetValue<bool>();

            if (alreadyOpened)
            {
                // If already opened, let the game do its normal "Empty Chest" animation
                return true; 
            }

            // It's a fresh chest!
            
            // Access the protected GetUniqueID method using Harmony's Traverse or Reflection
            // (Since it is protected, we can't call __instance.GetUniqueID() directly in C#)
            var uniqueId = Traverse.Create(__instance).Method("GetUniqueID").GetValue<string>();

            MelonLogger.Msg($"[Archipelago] Chest Opened! ID: {uniqueId}");
            
            // Delegate to the manager
            Properties.LocationManager.CheckLocation(uniqueId);

            // TODO: Send this ID to Archipelago!
            // DandaraArchipelagoMod.Session.Locations.CompleteLocationChecksAsync(...);

            // For now, return TRUE to let the game play the opening animation.
            // Later, we might return FALSE if we want to stop the game from giving the vanilla item.
            return true; 
        }
    }*/
}
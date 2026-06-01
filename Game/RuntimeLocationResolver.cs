/*
 * ArchDandara documentation
 * Purpose: Resolves runtime object interactions to AP locations when static aliases are not enough.
 * Why: Generated Unity names vary, so runtime matching reduces one-off hardcoded fixes.
 * Notes: Prefer aliases and structured matching over raw string guesses when adding new runtime locations.
 */

using System.Collections.Generic;
using ArchDandara.Archipelago;
using ArchDandara.Patches;
using UnityEngine;

namespace ArchDandara.Game
{
    public static class RuntimeLocationResolver
    {
        private static bool LoggedPowerupScanFailure;
        private static bool LoggedNpcScanFailure;

        public static bool TryResolveNpcInteraction(DialogueInteractable target, string roomName, out long locationId)
        {
            long[] candidates = LocationIds.GetLocationIdsForRoomType(roomName, "NPC");
            if (candidates.Length == 1)
            {
                // Single-check rooms do not need object ordering. This keeps simple NPC rooms from
                // depending on fragile Unity hierarchy names.
                locationId = candidates[0];
                return true;
            }

            if (candidates.Length == 0)
            {
                locationId = 0;
                return false;
            }

            List<RuntimeNpcEntry> entries = GetNpcEntries();
            if (entries.Count != candidates.Length)
            {
                // If the room scan and APWorld disagree, fail closed and let logs show an unmapped
                // interaction instead of guessing the wrong NPC check.
                locationId = 0;
                return false;
            }

            entries.Sort(CompareNpcEntries);

            for (int i = 0; i < entries.Count; i++)
            {
                if (object.ReferenceEquals(entries[i].Interactable, target))
                {
                    locationId = candidates[i];
                    return true;
                }
            }

            locationId = 0;
            return false;
        }

        public static bool TryResolvePowerupChest(PowerupInteractable target, string roomName, out long locationId)
        {
            long[] candidates = LocationIds.GetLocationIdsForRoomType(roomName, "Chest");
            if (candidates.Length == 1)
            {
                // Most rooms only have one AP chest. Fast-pathing those avoids unnecessary scene scans.
                locationId = candidates[0];
                return true;
            }

            if (candidates.Length == 0)
            {
                locationId = 0;
                return false;
            }

            List<RuntimePowerupEntry> entries = GetPowerupChestEntries();
            if (entries.Count != candidates.Length)
            {
                // Chest order is only safe when every AP chest in the room has a matching runtime
                // interactable. Otherwise we would risk sending the wrong location id.
                locationId = 0;
                return false;
            }

            entries.Sort(ComparePowerupEntries);

            for (int i = 0; i < entries.Count; i++)
            {
                if (object.ReferenceEquals(entries[i].Interactable, target))
                {
                    locationId = candidates[i];
                    return true;
                }
            }

            locationId = 0;
            return false;
        }

        private static List<RuntimePowerupEntry> GetPowerupChestEntries()
        {
            List<RuntimePowerupEntry> entries = new List<RuntimePowerupEntry>();

            try
            {
                Object[] objects = Object.FindObjectsOfType(typeof(PowerupInteractable));
                for (int i = 0; i < objects.Length; i++)
                {
                    PowerupInteractable interactable = objects[i] as PowerupInteractable;
                    if (object.ReferenceEquals(interactable, null) || interactable is WeaponAltar)
                        continue;

                    // Weapon altars share the same base class as chests but use separate logic and
                    // should never be mixed into chest ordering.
                    string objectName = LocationName.ForPowerupInteractable(interactable);
                    if (objectName.IndexOf("Chest") < 0 && objectName.IndexOf("chest") < 0)
                        continue;

                    RuntimePowerupEntry entry = new RuntimePowerupEntry();
                    entry.Interactable = interactable;
                    entry.Key = objectName;
                    entries.Add(entry);
                }
            }
            catch (System.Exception ex)
            {
                if (!LoggedPowerupScanFailure)
                {
                    LoggedPowerupScanFailure = true;
                    MLLog.Warning("[RuntimeLocationResolver] Failed to scan powerup chests: " +
                                        ex.GetType().Name + ": " + ex.Message);
                }
            }

            return entries;
        }

        private static List<RuntimeNpcEntry> GetNpcEntries()
        {
            List<RuntimeNpcEntry> entries = new List<RuntimeNpcEntry>();

            try
            {
                Object[] objects = Object.FindObjectsOfType(typeof(DialogueInteractable));
                for (int i = 0; i < objects.Length; i++)
                {
                    DialogueInteractable interactable = objects[i] as DialogueInteractable;
                    if (object.ReferenceEquals(interactable, null))
                        continue;

                    RuntimeNpcEntry entry = new RuntimeNpcEntry();
                    entry.Interactable = interactable;
                    entry.Key = LocationName.ForObject(interactable, "UNKNOWN_NPC");
                    entries.Add(entry);
                }
            }
            catch (System.Exception ex)
            {
                if (!LoggedNpcScanFailure)
                {
                    LoggedNpcScanFailure = true;
                    MLLog.Warning("[RuntimeLocationResolver] Failed to scan NPC interactables: " +
                                        ex.GetType().Name + ": " + ex.Message);
                }
            }

            return entries;
        }

        private class RuntimePowerupEntry
        {
            public PowerupInteractable Interactable;
            public string Key;
        }

        private class RuntimeNpcEntry
        {
            public DialogueInteractable Interactable;
            public string Key;
        }

        private static int ComparePowerupEntries(RuntimePowerupEntry a, RuntimePowerupEntry b)
        {
            return a.Key.CompareTo(b.Key);
        }

        private static int CompareNpcEntries(RuntimeNpcEntry a, RuntimeNpcEntry b)
        {
            return a.Key.CompareTo(b.Key);
        }
    }
}

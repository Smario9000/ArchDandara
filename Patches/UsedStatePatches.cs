/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for used/opened object state.
 * Why: Checked AP locations should stay visually open or used when saves replay or rooms reload.
 * Notes: Used-state patches make replayed checks visible in rooms, but should not mark unchecked objects used.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(ZoomAndFunctionChest), "IsUsed")]
    public static class ZoomAndFunctionChestIsUsedPatch
    {
        private static void Postfix(ZoomAndFunctionChest __instance, ref bool __result)
        {
            if (__result || __instance == null)
                return;

            string objectName = LocationName.ForChest(__instance);

            if (APLocationSender.IsChecked("Chest", GameAccess.CurrentRoomName, objectName))
                __result = true;
        }
    }

    [HarmonyPatch(typeof(PowerupInteractable), "IsUsed")]
    public static class PowerupInteractableIsUsedPatch
    {
        private static void Postfix(PowerupInteractable __instance, ref bool __result)
        {
            if (__instance == null || __instance is WeaponAltar)
                return;

            string objectName = LocationName.ForPowerupInteractable(__instance);
            string roomName = GameAccess.CurrentRoomName;
            long locationId;
            bool checkedLocation = APLocationSender.TryResolveLocationId("PowerupInteractable", roomName, objectName,
                                       out locationId) && SaveSync.HasCheckedLocation(locationId);

            if (!checkedLocation &&
                RuntimeLocationResolver.TryResolvePowerupChest(__instance, roomName, out locationId))
                checkedLocation = SaveSync.HasCheckedLocation(locationId);

            if (IsDreamStoneAltar(roomName, objectName))
            {
                __result = checkedLocation;
                return;
            }

            if (__result)
                return;

            if (checkedLocation)
                __result = true;
        }

        private static bool IsDreamStoneAltar(string roomName, string objectName)
        {
            if (roomName == "A4_DreamStone")
                return true;

            if (objectName == null)
                return false;

            return objectName.IndexOf("PU_Stone_Dreams", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Pearl of Dreams", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("A4_DreamStone", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

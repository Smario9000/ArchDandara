/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for powerup and chest interactables.
 * Why: Chests are the main AP check source, so interaction must be reported while unwanted vanilla rewards are blocked.
 * Notes: Chest patches send locations on interaction, then rely on powerup patches to stop the vanilla reward.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(ChestInteractable), "Interact")]
    public static class ChestInteractPatch
    {
        private static bool Prefix(ChestInteractable __instance)
        {
            if (__instance == null)
                return true;

            if (InteractionGateService.ShouldBlockChest(GameAccess.CurrentScene))
                return false;

            if (LocationName.TryGetChestIsUsed(__instance))
                return true;

            string objectName = LocationName.ForChest(__instance);
            string roomName = GameAccess.CurrentRoomName;
            long locationId;
            if (!APLocationSender.TryResolveLocationId("Chest", roomName, objectName, out locationId))
            {
                MLLog.Warning("[Patch][Chest] Unmapped, allowing vanilla interaction: " + roomName + " | " +
                                    objectName);
                return true;
            }

            APLocationSender.TrySend(locationId, LocationKey.Build("Chest", roomName, objectName));
            MLLog.Msg("[Patch][Chest] Blocked vanilla chest reward: " + roomName + " | " + objectName +
                            " | location=" + locationId);

            return false;
        }
    }

    [HarmonyPatch(typeof(PowerupInteractable), "Interact")]
    public static class PowerupInteractablePatch
    {
        private static bool Prefix(PowerupInteractable __instance, ref bool __state)
        {
            __state = false;

            if (__instance == null)
                return true;

            WeaponAltar weaponAltar = __instance as WeaponAltar;
            if (!object.ReferenceEquals(weaponAltar, null))
                return WeaponAltarState.HandleInteract(weaponAltar);

            if (__instance is WeaponAltar)
                return true;

            if (InteractionGateService.ShouldBlockChest(GameAccess.CurrentScene))
                return false;

            if (GrantContext.IsArchipelagoGrant)
            {
                LocationName.ForcePowerupInteractableUsedVisual(__instance);
                return false;
            }

            string objectName = LocationName.ForPowerupInteractable(__instance);
            string roomName = GameAccess.CurrentRoomName;
            long locationId;
            bool mapped = APLocationSender.TryResolveLocationId("PowerupInteractable", roomName, objectName,
                              out locationId) ||
                          RuntimeLocationResolver.TryResolvePowerupChest(__instance, roomName, out locationId);

            bool checkedAlready = mapped && SaveSync.HasCheckedLocation(locationId);
            bool vanillaUsed = LocationName.TryGetPowerupInteractableIsUsed(__instance);
            if (vanillaUsed && (!mapped || checkedAlready))
            {
                LocationName.ForcePowerupInteractableUsedVisual(__instance);
                return false;
            }

            bool sent = false;
            if (mapped)
            {
                string key = LocationKey.Build("PowerupInteractable", roomName, objectName);
                sent = APLocationSender.TrySend(locationId, key);
                checkedAlready = SaveSync.HasCheckedLocation(locationId);
            }

            if (mapped)
            {
                LocationName.ForcePowerupInteractableUsedVisual(__instance);
                MLLog.Msg("[Patch][PowerupInteractable] Interaction: " + roomName + " | " +
                                objectName + " | location=" + locationId + " | sent=" + sent +
                                " | checked=" + checkedAlready + " | vanillaUsed=" + vanillaUsed);
                return false;
            }

            MLLog.Warning("[Patch][PowerupInteractable] Unmapped, allowing vanilla interaction: " +
                                roomName + " | " + objectName);

            GrantContext.IsVanillaPowerupInteraction = true;
            __state = true;
            return true;
        }

        private static void Postfix(bool __state)
        {
            if (__state)
                GrantContext.IsVanillaPowerupInteraction = false;
        }

        private static void Finalizer(bool __state)
        {
            if (__state)
                GrantContext.IsVanillaPowerupInteraction = false;
        }
    }
}

/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for weapon altars and altar switchable state.
 * Why: Altars can give weapons through special flows, so AP needs reliable sending and vanilla reward blocking.
 * Notes: Weapon altar patches handle both visual switch state and interaction reward blocking.
 */

using ArchDandara.Archipelago;
using ArchDandara.Game;
using HarmonyLib;
using UnityEngine;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(InteractorController), "Interact")]
    public static class A1Gd14InteractorControllerInteractPatch
    {
        private static void Prefix(InteractorController __instance)
        {
            if (object.ReferenceEquals(__instance, null))
                return;

            IInteractable interactable = __instance.GetInteractable();
            SpecialNpcChecks.TrySendCreationStoneInteraction(interactable, "InteractorController.Interact");

            if (GameAccess.CurrentScene != "A1_GD14")
                return;

            JonnyBMissileFallbackService.TrySendFromInteraction(interactable, "InteractorController.Interact");
        }
    }

    [HarmonyPatch(typeof(PowerupInteractable), "Interact")]
    public static class PowerupInteractableWeaponAltarInteractPatch
    {
        private static bool Prefix(PowerupInteractable __instance)
        {
            WeaponAltar altar = __instance as WeaponAltar;
            if (object.ReferenceEquals(altar, null))
                return true;

            if (GameAccess.CurrentScene != "A1_GD14")
                return true;

            WeaponAltarState.LogA1Gd14(altar, "PowerupInteractable.Interact allowing vanilla; AP check uses Char_GameDev");
            return true;
        }
    }

    [HarmonyPatch(typeof(WeaponAltar), "WhatToHappenOnInteract")]
    public static class WeaponAltarPatch
    {
        private static bool Prefix(WeaponAltar __instance)
        {
            if (__instance == null)
                return true;

            if (GrantContext.IsArchipelagoGrant)
                return true;

            if (GameAccess.CurrentScene == "A1_GD14")
            {
                WeaponAltarState.LogA1Gd14(__instance,
                    "WhatToHappenOnInteract allowing vanilla; AP check uses Char_GameDev");
                return true;
            }

            string objectName = WeaponAltarState.GetObjectName(__instance);
            string roomName = GameAccess.CurrentRoomName;

            long locationId;
            if (!APLocationSender.TryResolveLocationId("WeaponAltar", roomName, objectName, out locationId))
            {
                MLLog.Warning("[Patch][WeaponAltar] Unmapped, allowing vanilla interaction: scene=" +
                                    GameAccess.CurrentScene + " | room=" + roomName + " | " + objectName);
                return true;
            }

            bool sent = APLocationSender.TrySend(locationId, LocationKey.Build("WeaponAltar", roomName, objectName));
            WeaponAltarState.SetSwitchableState(__instance, false);

            MLLog.Msg("[Patch][WeaponAltar] Blocked vanilla altar reward: scene=" + GameAccess.CurrentScene +
                            " | room=" + roomName + " | " + objectName + " | location=" + locationId +
                            " | sent=" + sent);

            return false;
        }
    }

    [HarmonyPatch(typeof(WeaponAltar), "IsUsed")]
    public static class WeaponAltarIsUsedPatch
    {
        private static void Postfix(WeaponAltar __instance, ref bool __result)
        {
            if (__instance == null)
                return;

            long locationId;
            if (!WeaponAltarState.TryResolve(__instance, out locationId))
                return;

            __result = SaveSync.HasCheckedLocation(locationId);
        }
    }

    [HarmonyPatch(typeof(WeaponAltar), "UpdateSwitchable")]
    public static class WeaponAltarUpdateSwitchablePatch
    {
        private static bool Prefix(WeaponAltar __instance)
        {
            if (__instance == null)
                return true;

            long locationId;
            if (!WeaponAltarState.TryResolve(__instance, out locationId))
            {
                WeaponAltarState.LogA1Gd14(__instance, "UpdateSwitchable unmapped");
                return true;
            }

            bool checkedLocation = SaveSync.HasCheckedLocation(locationId);
            WeaponAltarState.SetSwitchableState(__instance, !checkedLocation);
            WeaponAltarState.LogA1Gd14(__instance, "UpdateSwitchable checked=" + checkedLocation +
                                                   " location=" + locationId);
            return false;
        }
    }

    [HarmonyPatch(typeof(WeaponAltar), "Start")]
    public static class WeaponAltarStartPatch
    {
        private static void Postfix(WeaponAltar __instance)
        {
            WeaponAltarState.LogA1Gd14(__instance, "Start");
        }
    }

    internal static class WeaponAltarState
    {
        public static bool HandleInteract(WeaponAltar altar)
        {
            if (object.ReferenceEquals(altar, null))
                return true;

            if (GrantContext.IsArchipelagoGrant)
                return true;

            string objectName = GetObjectName(altar);
            string roomName = GameAccess.CurrentRoomName;

            long locationId;
            if (!APLocationSender.TryResolveLocationId("WeaponAltar", roomName, objectName, out locationId))
            {
                MLLog.Warning("[Patch][WeaponAltar] Unmapped inherited interaction, allowing vanilla: scene=" +
                                    GameAccess.CurrentScene + " | room=" + roomName + " | " + objectName);
                return true;
            }

            bool sent = APLocationSender.TrySend(locationId, LocationKey.Build("WeaponAltar", roomName, objectName));
            bool checkedAlready = SaveSync.HasCheckedLocation(locationId);
            if (sent || checkedAlready)
                SetSwitchableState(altar, false);

            MLLog.Msg("[Patch][WeaponAltar] Blocked inherited altar interaction: scene=" +
                            GameAccess.CurrentScene + " | room=" + roomName + " | " + objectName +
                            " | location=" + locationId + " | sent=" + sent +
                            " | checked=" + checkedAlready);
            return false;
        }

        public static string GetObjectName(WeaponAltar altar)
        {
            string objectName = LocationName.ForObject(altar, "UNKNOWN_WEAPON_ALTAR");
            string uniqueId = LocationName.TryGetPowerupInteractableUniqueId(altar);
            if (!string.IsNullOrEmpty(uniqueId))
                objectName = objectName + "#" + uniqueId;

            if (!object.ReferenceEquals(altar, null) && !object.ReferenceEquals(altar.weaponChar, null))
                objectName = objectName + "#" + altar.weaponChar.storyEvent;

            return objectName;
        }

        public static bool TryResolve(WeaponAltar altar, out long locationId)
        {
            return APLocationSender.TryResolveLocationId("WeaponAltar", GameAccess.CurrentRoomName,
                GetObjectName(altar), out locationId);
        }

        public static void SetSwitchableState(WeaponAltar altar, bool active)
        {
            if (object.ReferenceEquals(altar, null))
                return;

            Switchable switchable = altar.GetComponent<Switchable>();
            if (!object.ReferenceEquals(switchable, null))
                switchable.SetState(active);

            Animator animator = altar.GetComponent<Animator>();
            if (!object.ReferenceEquals(animator, null))
                animator.SetBool("On", active);
        }

        public static void LogA1Gd14(WeaponAltar altar, string action)
        {
            if (GameAccess.CurrentScene != "A1_GD14")
                return;

            string storyEvent = "None";
            if (!object.ReferenceEquals(altar, null) && !object.ReferenceEquals(altar.weaponChar, null))
                storyEvent = altar.weaponChar.storyEvent.ToString();

            MLLog.Msg("[Patch][WeaponAltar][A1_GD14] " + action + " | room=" +
                            GameAccess.CurrentRoomName + " | object=" + GetObjectName(altar) +
                            " | weaponEvent=" + storyEvent);
        }
    }
}

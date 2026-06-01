/*
 * ArchDandara documentation
 * Purpose: Diagnostic and refresh hooks for ammo HUD dependency flow.
 * Why: The ammo HUD depends on vanilla unlock order, so AP grants need repair points.
 * Notes: Most logging here is diagnostic; keep category controls in mind before adding more HUD spam.
 */

using ArchDandara.Game;
using ArchDandara.Archipelago;
using HarmonyLib;
using UnityEngine;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PlayerWeapons), "AddNewWeapon")]
    public static class PlayerWeaponsAddNewWeaponPatch
    {
        private static bool Prefix(PlayerWeapons __instance, StoryEvent weaponEvent)
        {
            if (GrantContext.IsArchipelagoGrant)
                return true;

            if (GameAccess.CurrentScene == "A1_GD14" && weaponEvent == StoryEvent.Weapon_Missile)
            {
                JonnyBMissileFallbackService.TrySendMissileAltarCheck(
                    "PlayerWeapons|A1_GD14|Weapon_Missile=>Missile Alter",
                    "PlayerWeapons.AddNewWeapon");
                MLLog.Msg("[Patch][PlayerWeapons] Blocked Jonny B. vanilla direct weapon add: " + weaponEvent);
                return false;
            }

            if (PlayerWeaponPatchLogic.PlayerAlreadyOwnsWeapon(weaponEvent))
                return true;

            if (PlayerWeaponPatchLogic.TrySendWeaponAltarLocation(weaponEvent))
            {
                MLLog.Msg("[Patch][PlayerWeapons] Blocked vanilla direct weapon add: " + weaponEvent);
                return false;
            }

            MLLog.Msg("[Patch][PlayerWeapons] Blocked unmapped vanilla direct weapon add: " + weaponEvent);
            return false;
        }

        private static void Postfix(PlayerWeapons __instance, StoryEvent weaponEvent)
        {
            if (object.ReferenceEquals(__instance, null))
                return;

            MLLog.Msg("[Patch][PlayerWeapons] AddNewWeapon " + weaponEvent +
                            " | equipped=" + __instance.CurrentEquippedWeaponStoryEvent);
            HudRefreshService.ForceAmmoHud();
        }
    }

    internal static class PlayerWeaponPatchLogic
    {
        public static bool PlayerAlreadyOwnsWeapon(StoryEvent weaponEvent)
        {
            try
            {
                StoryManager storyManager = PersistentSingleton<StoryManager>.instance;
                return !object.ReferenceEquals(storyManager, null) && storyManager.GetEvent(weaponEvent);
            }
            catch
            {
                return false;
            }
        }

        public static bool TrySendWeaponAltarLocation(StoryEvent weaponEvent)
        {
            long locationId;
            string roomName = GameAccess.CurrentRoomName;

            if (TrySendActiveAltar(weaponEvent, roomName, out locationId))
                return true;

            if (GameAccess.CurrentScene == "A1_GD14" && weaponEvent == StoryEvent.Weapon_Missile)
            {
                string objectName = "AltarMissile#A1_GD14#Weapon_Missile";
                if (APLocationSender.TryResolveLocationId("WeaponAltar", roomName, objectName, out locationId) ||
                    APLocationSender.TryResolveLocationId("WeaponAltar", "A1_GD14", objectName, out locationId))
                {
                    APLocationSender.TrySend(locationId, LocationKey.Build("WeaponAltar", roomName, objectName));
                    MLLog.Msg("[Patch][PlayerWeapons] Sent altar location from direct weapon add: " +
                                    locationId + " | " + roomName + " | " + objectName);
                    return true;
                }
            }

            return false;
        }

        private static bool TrySendActiveAltar(StoryEvent weaponEvent, string roomName, out long locationId)
        {
            locationId = 0;

            WeaponAltar[] altars = Resources.FindObjectsOfTypeAll<WeaponAltar>();
            for (int i = 0; i < altars.Length; i++)
            {
                WeaponAltar altar = altars[i];
                if (object.ReferenceEquals(altar, null) || object.ReferenceEquals(altar.weaponChar, null))
                    continue;

                if (altar.weaponChar.storyEvent != weaponEvent)
                    continue;

                string objectName = WeaponAltarState.GetObjectName(altar);
                if (!APLocationSender.TryResolveLocationId("WeaponAltar", roomName, objectName, out locationId))
                    continue;

                APLocationSender.TrySend(locationId, LocationKey.Build("WeaponAltar", roomName, objectName));
                WeaponAltarState.SetSwitchableState(altar, false);
                MLLog.Msg("[Patch][PlayerWeapons] Sent active altar location from direct weapon add: " +
                                locationId + " | " + roomName + " | " + objectName);
                return true;
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(HUDAmmo), "Awake")]
    public static class HudAmmoAwakePatch
    {
        private static void Postfix(HUDAmmo __instance)
        {
            HudAmmoPatchLog.LogHudAmmo("Awake", __instance);
        }
    }

    [HarmonyPatch(typeof(HUDAmmo), "OnEnable")]
    public static class HudAmmoOnEnablePatch
    {
        private static void Postfix(HUDAmmo __instance)
        {
            HudAmmoPatchLog.LogHudAmmo("OnEnable", __instance);
        }
    }

    [HarmonyPatch(typeof(HUDAmmo), "OnDisable")]
    public static class HudAmmoOnDisablePatch
    {
        private static void Prefix(HUDAmmo __instance)
        {
            HudAmmoPatchLog.LogHudAmmo("OnDisable", __instance);
        }
    }

    [HarmonyPatch(typeof(HUDScoreDependentEvent), "OnUpdateScore")]
    public static class HudScoreDependentEventPatch
    {
        private static void Postfix(HUDScoreDependentEvent __instance, int newScore)
        {
            if (!HudAmmoPatchLog.IsAmmoRelated(__instance))
                return;

            MLLog.Msg("[Patch][HUDAmmoFlow] HUDScoreDependentEvent score=" + newScore +
                            " | object=" + HudAmmoPatchLog.PathFor(__instance.transform));
        }
    }

    [HarmonyPatch(typeof(PowerupCountDependency), "OnEnable")]
    public static class PowerupCountDependencyOnEnablePatch
    {
        private static void Postfix(PowerupCountDependency __instance)
        {
            HudAmmoPatchLog.LogPowerupDependency("OnEnable", __instance);
        }
    }

    [HarmonyPatch(typeof(PowerupCountDependency), "IsUnlocked")]
    public static class PowerupCountDependencyIsUnlockedPatch
    {
        private static void Postfix(PowerupCountDependency __instance, ref bool __result)
        {
            if (__result)
                return;

            if (!HudRefreshService.ShouldForceAmmoDependency(__instance))
                return;

            __result = true;
            if (!HudAmmoPatchLog.ShouldLogForcedDependency())
                return;

            MLLog.Msg("[Patch][HUDAmmoFlow] Forced ammo dependency unlocked | event=" +
                            __instance.powerupEvent +
                            " | min=" + __instance.minPowerupCount +
                            " | object=" + HudAmmoPatchLog.PathFor(__instance.transform));
        }
    }

    [HarmonyPatch(typeof(PowerupCountDependency), "OnPowerupUnlocked")]
    public static class PowerupCountDependencyOnPowerupUnlockedPatch
    {
        private static void Postfix(PowerupCountDependency __instance, StoryEvent e, int count)
        {
            HudAmmoPatchLog.LogPowerupDependency("OnPowerupUnlocked " + e + " count=" + count, __instance);
            HudRefreshService.ForceAmmoHudDependencies();
        }
    }

    internal static class HudAmmoPatchLog
    {
        private static bool LoggedForcedDependency;

        public static void LogHudAmmo(string action, HUDAmmo hudAmmo)
        {
            if (object.ReferenceEquals(hudAmmo, null))
                return;

            MLLog.Msg("[Patch][HUDAmmo] " + action +
                            " | enabled=" + hudAmmo.enabled +
                            " | activeSelf=" + hudAmmo.gameObject.activeSelf +
                            " | activeInHierarchy=" + hudAmmo.gameObject.activeInHierarchy +
                            " | path=" + PathFor(hudAmmo.transform));
        }

        public static void LogPowerupDependency(string action, PowerupCountDependency dependency)
        {
            if (object.ReferenceEquals(dependency, null))
                return;

            if (!HudRefreshService.ShouldShowAmmoHud(dependency.powerupEvent))
                return;

            if (action == "OnEnable" && HudRefreshService.ShouldForceAmmoDependency(dependency))
                return;

            MLLog.Msg("[Patch][HUDAmmoFlow] PowerupCountDependency " + action +
                            " | event=" + dependency.powerupEvent +
                            " | onWhenEvent=" + dependency.onWhenEvent +
                            " | min=" + dependency.minPowerupCount +
                            " | object=" + PathFor(dependency.transform));
        }

        public static bool ShouldLogForcedDependency()
        {
            if (LoggedForcedDependency)
                return false;

            LoggedForcedDependency = true;
            return true;
        }

        public static bool IsAmmoRelated(Component component)
        {
            if (object.ReferenceEquals(component, null))
                return false;

            string path = PathFor(component.transform).ToLower();
            return path.IndexOf("ammo") >= 0 || path.IndexOf("weapon") >= 0 || path.IndexOf("hud") >= 0;
        }

        public static string PathFor(Transform transform)
        {
            if (object.ReferenceEquals(transform, null))
                return "null";

            string path = transform.name;
            Transform parent = transform.parent;
            while (!object.ReferenceEquals(parent, null))
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}

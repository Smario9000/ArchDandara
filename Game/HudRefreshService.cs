/*
 * ArchDandara documentation
 * Purpose: Forces HUD refreshes for weapons, ammo, shield, and related AP grants.
 * Why: The HUD normally follows vanilla unlock order, while AP item order can be arbitrary.
 * Notes: HUD refreshes may need to run more than once because Unity UI dependencies enable over several frames.
 */

using HarmonyLib;
using UnityEngine;

namespace ArchDandara.Game
{
    public static class HudRefreshService
    {
        private static bool LoggedSwipeRefreshFailure;
        private static bool LoggedAmmoHudForced;
        private static bool LoggedAmmoDependenciesRefreshed;
        private static float NextAmmoKeepAlive;

        public static void RefreshAfterWeaponGrant(StoryEvent eventId)
        {
            if (!ShouldShowAmmoHud(eventId))
                return;

            RefreshWeaponMenus();
            ForceAmmoHud();
            RefreshPlayerGun();
            MLLog.Msg("[HUDRefresh] Refreshed weapon HUD for " + eventId);
        }

        public static void Update()
        {
            if (UnityEngine.Time.time < NextAmmoKeepAlive)
                return;

            NextAmmoKeepAlive = UnityEngine.Time.time + 0.5f;

            if (!PlayerHasAmmoHudPower())
                return;

            ForceAmmoHud();
        }

        private static void RefreshWeaponMenus()
        {
            try
            {
                System.Reflection.MethodInfo getOptionsMethod =
                    AccessTools.Method(typeof(HUDSwipeSwitchable), "GetSwipeMenuOptions");

                Object[] objects = Resources.FindObjectsOfTypeAll(typeof(HUDSwipeSwitchable));
                for (int i = 0; i < objects.Length; i++)
                {
                    HUDSwipeSwitchable swipe = objects[i] as HUDSwipeSwitchable;
                    if (object.ReferenceEquals(swipe, null))
                        continue;

                    if (!object.ReferenceEquals(swipe.gameObject, null) && !swipe.gameObject.activeSelf)
                        swipe.gameObject.SetActive(true);

                    if (!object.ReferenceEquals(getOptionsMethod, null))
                        getOptionsMethod.Invoke(swipe, null);
                }
            }
            catch (System.Exception ex)
            {
                if (!LoggedSwipeRefreshFailure)
                {
                    LoggedSwipeRefreshFailure = true;
                    MLLog.Warning("[HUDRefresh] Failed to rebuild weapon HUD menus: " +
                                        ex.GetType().Name + ": " + ex.Message);
                }
            }
        }

        public static void ForceAmmoHud()
        {
            ForceAmmoHudDependencies();

            Object[] objects = Resources.FindObjectsOfTypeAll(typeof(HUDAmmo));
            int forced = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                HUDAmmo hudAmmo = objects[i] as HUDAmmo;
                if (object.ReferenceEquals(hudAmmo, null))
                    continue;

                EnsureParentsActive(hudAmmo.transform);
                hudAmmo.enabled = true;
                hudAmmo.SetState(true);
                hudAmmo.TurnOnImmediate();
                forced++;
            }

            if (forced > 0 && !LoggedAmmoHudForced)
            {
                LoggedAmmoHudForced = true;
                MLLog.Msg("[HUDRefresh] Ammo HUD keep-alive forced objects=" + forced);
            }
        }

        public static void ForceAmmoHudDependencies()
        {
            if (!SaveSync.HasAmmoHudBootstrap())
                return;

            Object[] objects = Resources.FindObjectsOfTypeAll(typeof(PowerupCountDependency));
            int refreshed = 0;
            for (int i = 0; i < objects.Length; i++)
            {
                PowerupCountDependency dependency = objects[i] as PowerupCountDependency;
                if (!ShouldForceAmmoDependency(dependency))
                    continue;

                EnsureParentsActive(dependency.transform);
                dependency.OnEnable();
                refreshed++;
            }

            if (refreshed > 0 && !LoggedAmmoDependenciesRefreshed)
            {
                LoggedAmmoDependenciesRefreshed = true;
                MLLog.Msg("[HUDRefresh] Ammo HUD dependencies refreshed=" + refreshed);
            }
        }

        public static bool ShouldForceAmmoDependency(PowerupCountDependency dependency)
        {
            if (object.ReferenceEquals(dependency, null) || !SaveSync.HasAmmoHudBootstrap())
                return false;

            if (dependency.powerupEvent != StoryEvent.PU_ManaWeapon &&
                dependency.powerupEvent != StoryEvent.PU_FearWeapon &&
                dependency.powerupEvent != StoryEvent.PU_Ammo)
                return false;

            string path = PathFor(dependency.transform).ToLower();
            return path.IndexOf("ammo") >= 0 ||
                   path.IndexOf("weapon") >= 0 ||
                   path.IndexOf("hud") >= 0;
        }

        private static void EnsureParentsActive(Transform transform)
        {
            while (!object.ReferenceEquals(transform, null))
            {
                if (!object.ReferenceEquals(transform.gameObject, null) && !transform.gameObject.activeSelf)
                    transform.gameObject.SetActive(true);

                if (!object.ReferenceEquals(transform.GetComponent<HUDManager>(), null))
                    return;

                transform = transform.parent;
            }
        }

        private static string PathFor(Transform transform)
        {
            if (object.ReferenceEquals(transform, null))
                return "";

            string path = transform.name;
            Transform parent = transform.parent;
            while (!object.ReferenceEquals(parent, null))
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static void RefreshPlayerGun()
        {
            PlayerController player = GameAccess.Player;
            if (!object.ReferenceEquals(player, null))
            {
                if (!object.ReferenceEquals(player.Weapons, null) &&
                    ShouldShowAmmoHud(player.Weapons.CurrentEquippedWeaponStoryEvent))
                    player.Weapons.AddNewWeapon(player.Weapons.CurrentEquippedWeaponStoryEvent);

                if (!object.ReferenceEquals(player.Gun, null))
                    player.Gun.ResetState();
            }

            HUDManager hudManager = GameAccess.HudManager;
            if (!object.ReferenceEquals(hudManager, null) && !hudManager.IsHidingPermanently())
                hudManager.Show();
        }

        public static bool ShouldShowAmmoHud(StoryEvent eventId)
        {
            return eventId == StoryEvent.Weapon_Missile ||
                   eventId == StoryEvent.Weapon_EnergyBall ||
                   eventId == StoryEvent.Weapon_Remembrance ||
                   eventId == StoryEvent.Weapon_Bounce ||
                   eventId == StoryEvent.Weapon_Boomerang ||
                   eventId == StoryEvent.Weapon_Vaccum ||
                   eventId == StoryEvent.Weapon_WaterBomb ||
                   eventId == StoryEvent.Weapon_Teleport ||
                   eventId == StoryEvent.Weapon_Firewall ||
                   eventId == StoryEvent.PU_Shield ||
                   eventId == StoryEvent.PU_Ammo ||
                   eventId == StoryEvent.PU_ManaWeapon ||
                   eventId == StoryEvent.PU_FearWeapon;
        }

        private static bool PlayerHasAmmoHudPower()
        {
            StoryManager storyManager = GameAccess.StoryManager;
            if (object.ReferenceEquals(storyManager, null))
                return false;

            return storyManager.GetEvent(StoryEvent.Weapon_Missile) ||
                   storyManager.GetEvent(StoryEvent.Weapon_EnergyBall) ||
                   storyManager.GetEvent(StoryEvent.Weapon_Remembrance) ||
                   storyManager.GetEvent(StoryEvent.Weapon_Bounce) ||
                   storyManager.GetEvent(StoryEvent.Weapon_Boomerang) ||
                   storyManager.GetEvent(StoryEvent.Weapon_Vaccum) ||
                   storyManager.GetEvent(StoryEvent.Weapon_WaterBomb) ||
                   storyManager.GetEvent(StoryEvent.Weapon_Teleport) ||
                   storyManager.GetEvent(StoryEvent.Weapon_Firewall) ||
                   storyManager.GetEvent(StoryEvent.PU_Shield) ||
                   storyManager.GetEvent(StoryEvent.PU_ManaWeapon) ||
                   storyManager.GetEvent(StoryEvent.PU_FearWeapon) ||
                   SaveSync.HasAmmoHudBootstrap();
        }
    }
}

/*
 * ArchDandara documentation
 * Purpose: Harmony hooks for detecting player death causes and applying incoming DeathLink deaths.
 * Why: DeathLink needs game-aware messages and must invoke death routines safely.
 * Notes: DeathLink patches must avoid recursion: incoming AP deaths should not immediately send a second outgoing death.
 */

using ArchDandara.Archipelago;
using ArchDandara.Config;
using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PlayerController), "OnHealthChange")]
    public static class PlayerControllerDeathLinkHealthChangePatch
    {
        private static void Prefix(HealthEffectType type, float amount, IHealthContainer healthContainer,
            HealthChanger origin)
        {
            DeathLinkCauseTracker.RecordDamage(type, amount, origin);
        }
    }

    [HarmonyPatch(typeof(PlayerController), "OnPlayerDeath")]
    public static class PlayerControllerDeathLinkDeathPatch
    {
        private static void Postfix(HealthEffectType type, IHealthContainer fighter)
        {
            if (APDeathLink.IsReceivingDeathLink)
                return;

            string cause = DeathLinkCauseTracker.BuildDeathCause(type);
            APClient.SendDeath(cause);
        }
    }

    internal static class DeathLinkCauseTracker
    {
        private static string LastDamageSource;
        private static HealthEffectType LastDamageType;

        public static void RecordDamage(HealthEffectType type, float amount, HealthChanger origin)
        {
            if (amount < 0f)
                return;

            if (type != HealthEffectType.Damage &&
                type != HealthEffectType.DamageExplosion &&
                type != HealthEffectType.UnparriableDamage &&
                type != HealthEffectType.DamageAbsoluteValue &&
                type != HealthEffectType.InstantDeath)
                return;

            LastDamageType = type;
            LastDamageSource = DescribeOrigin(origin);
        }

        public static string BuildDeathCause(HealthEffectType deathType)
        {
            string source = LastDamageSource;
            if (string.IsNullOrEmpty(source))
                source = "unknown danger";

            string room = GameAccess.CurrentRoomName;
            if (string.IsNullOrEmpty(room))
                room = GameAccess.CurrentScene;
            if (string.IsNullOrEmpty(room))
                room = "an unknown room";

            return APConfig.SlotName + " died to " + source + " in " + room + " (" + deathType + ").";
        }

        private static string DescribeOrigin(HealthChanger origin)
        {
            if (object.ReferenceEquals(origin, null))
                return "unknown danger";

            string name = "unknown danger";
            try
            {
                if (!object.ReferenceEquals(origin.gameObject, null) &&
                    !string.IsNullOrEmpty(origin.gameObject.name))
                    name = origin.gameObject.name;
            }
            catch
            {
            }

            try
            {
                string parentName = FindUsefulParentName(origin.transform);
                if (!string.IsNullOrEmpty(parentName) && parentName != name)
                    name = parentName + "/" + name;
            }
            catch
            {
            }

            MLLog.Msg("[DeathLink] Last damage source: " + name + " | type=" + LastDamageType);
            return name;
        }

        private static string FindUsefulParentName(UnityEngine.Transform transform)
        {
            if (object.ReferenceEquals(transform, null))
                return null;

            UnityEngine.Transform parent = transform.parent;
            while (!object.ReferenceEquals(parent, null))
            {
                if (!string.IsNullOrEmpty(parent.name) &&
                    parent.name != "PooledObjects" &&
                    parent.name != "Projectiles")
                    return parent.name;

                parent = parent.parent;
            }

            return null;
        }
    }
}

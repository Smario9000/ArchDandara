/*
 * ArchDandara documentation
 * Purpose: Harmony hooks that let AP damage upgrades affect spawned projectiles and player damage systems.
 * Why: The game recreates weapons and projectiles often, so upgrades must be reapplied at runtime.
 * Notes: Keep these patches narrow because they touch combat math and can affect every projectile in the game.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(Gun), "Shoot")]
    public static class GunShootDamageUpgradePatch
    {
        private static void Postfix(Gun __instance, Shootable __result)
        {
            DamageUpgradeService.MarkShootable(__instance, __result);
        }
    }

    [HarmonyPatch(typeof(HealthChanger), "DamageHealthContainer")]
    public static class HealthChangerDamageUpgradePatch
    {
        private static void Prefix(HealthChanger __instance, IHealthContainer health, ref int __state)
        {
            __state = 0;
            if (__instance == null)
                return;

            int scaled = DamageUpgradeService.ScaleDamage(__instance, health, __instance.effectAmount);
            if (scaled == __instance.effectAmount)
                return;

            __state = __instance.effectAmount;
            __instance.effectAmount = scaled;
        }

        private static void Postfix(HealthChanger __instance, int __state)
        {
            if (__state > 0 && __instance != null)
                __instance.effectAmount = __state;
        }

        private static void Finalizer(HealthChanger __instance, int __state)
        {
            if (__state > 0 && __instance != null)
                __instance.effectAmount = __state;
        }
    }

    [HarmonyPatch(typeof(HealthContainer), "Change")]
    public static class HealthContainerSaltsAwarenessDamagePatch
    {
        private static void Prefix(IHealthContainer __instance, HealthEffectType type, ref int amount)
        {
            if (type != HealthEffectType.Damage &&
                type != HealthEffectType.DamageExplosion &&
                type != HealthEffectType.UnparriableDamage &&
                type != HealthEffectType.DamageAbsoluteValue)
                return;

            amount = DamageUpgradeService.ScaleIncomingPlayerDamage(__instance, amount);
        }
    }

    [HarmonyPatch(typeof(HealthContainerSharedMultiplierDamage), "GetMultiplier")]
    public static class SaltsAwarenessDamageMultiplierPatch
    {
        private static void Postfix(ref float __result)
        {
            __result = DamageUpgradeService.ScaleSaltsAwarenessDamageMultiplier(__result);
        }
    }

    [HarmonyPatch(typeof(SpendMoneyOverTimeController), "End")]
    public static class SaltsAwarenessForcedEndPatch
    {
        private static bool Prefix(SpendMoneyOverTimeController __instance,
            SpendMoneyOverTimeController.EndReason reason)
        {
            return !DamageUpgradeService.ShouldPreventForcedSaltsAwarenessEnd(__instance, reason);
        }

        private static void Postfix(SpendMoneyOverTimeController __instance)
        {
            if (DamageUpgradeService.IsPlayerSuperDandaraController(__instance))
                DamageUpgradeService.RefreshSaltsAwarenessHue();
        }
    }

    [HarmonyPatch(typeof(SpendMoneyOverTimeController), "Begin")]
    public static class SaltsAwarenessCostPatch
    {
        private static readonly System.Collections.Generic.Dictionary<int, int> BaseCostPerSecond =
            new System.Collections.Generic.Dictionary<int, int>();

        private static void Prefix(SpendMoneyOverTimeController __instance)
        {
            if (__instance == null || !DamageUpgradeService.IsPlayerSuperDandaraController(__instance))
                return;

            System.Reflection.FieldInfo field = AccessTools.Field(typeof(SpendMoneyOverTimeController),
                "_moneyCostPerSecond");
            if (object.ReferenceEquals(field, null))
                return;

            int id = __instance.GetInstanceID();
            int baseCost;
            if (!BaseCostPerSecond.TryGetValue(id, out baseCost))
            {
                baseCost = (int)field.GetValue(__instance);
                BaseCostPerSecond[id] = baseCost;
            }

            field.SetValue(__instance, DamageUpgradeService.ScaleSaltsAwarenessCostPerSecond(baseCost));
        }

        private static void Postfix(SpendMoneyOverTimeController __instance)
        {
            if (DamageUpgradeService.IsPlayerSuperDandaraController(__instance))
                DamageUpgradeService.RefreshSaltsAwarenessHue();
        }
    }
}

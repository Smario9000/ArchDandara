/*
 * ArchDandara documentation
 * Purpose: Tracks and applies AP damage upgrades for arrows, weapons, and Salts Awareness.
 * Why: Damage upgrades are count-based AP items and must be reapplied after death, reload, and projectile recreation.
 * Notes: The service tags damage sources it has already adjusted so repeated refreshes do not stack scaling accidentally.
 */

using System.Collections.Generic;
using ArchDandara.Archipelago;
using UnityEngine;

namespace ArchDandara.Game
{
    public static class DamageUpgradeService
    {
        public const string ArrowDamageUpgradeItem = "Dandara Arrow Damage Upgrade";
        public const string WeaponDamageUpgradeItem = "Dandara Weapon Damage Upgrade";
        public const string SaltsAwarenessUpgradeItem = "Salt's Awareness Upgrade";

        private const string PlayerProjectileTag = "ArchDandaraPlayerDamageType";
        private const string ArrowProjectileType = "Arrow";
        private const string WeaponProjectileType = "Weapon";
        private static readonly Dictionary<int, Color> BaseRendererColors = new Dictionary<int, Color>();
        private static readonly Dictionary<int, Color> BaseSaltsAwarenessRendererColors = new Dictionary<int, Color>();

        public static int ArrowUpgradeCount
        {
            get { return Clamp(SaveSync.GetReceivedItemCount(ArrowDamageUpgradeItem), 0, APSlotSettings.ArrowDamageUpgradeLimit); }
        }

        public static int WeaponUpgradeCount
        {
            get { return Clamp(SaveSync.GetReceivedItemCount(WeaponDamageUpgradeItem), 0, APSlotSettings.WeaponDamageUpgradeLimit); }
        }

        public static bool HasSaltsAwarenessUpgrade
        {
            get
            {
                return APSlotSettings.SaltsAwarenessUpgradeInPool &&
                       GetSaltsAwarenessTotalCount() > 1;
            }
        }

        public static bool HasAnySaltsAwarenessItem
        {
            get { return GetSaltsAwarenessTotalCount() > 0; }
        }

        public static bool IsSaltsAwarenessActive
        {
            get
            {
                PlayerController player = GameAccess.Player;
                return player != null && player.superDandaraController != null &&
                       player.superDandaraController.IsSpending();
            }
        }

        public static void MarkShootable(Gun origin, Shootable shootable)
        {
            if (shootable == null)
                return;

            TaggedPlayerDamage tagged = shootable.GetComponent<TaggedPlayerDamage>();
            if (origin == null || !IsPlayerGun(origin))
            {
                ClearTaggedDamage(shootable);
                RestoreHue(shootable.GetComponentsInChildren<SpriteRenderer>(true), BaseRendererColors);
                return;
            }

            string damageType = GetCurrentPlayerDamageType(shootable);
            if (string.IsNullOrEmpty(damageType))
            {
                ClearTaggedDamage(shootable);
                RestoreHue(shootable.GetComponentsInChildren<SpriteRenderer>(true), BaseRendererColors);
                return;
            }

            StoryEvent weapon = GetCurrentPlayerWeapon();
            SetTaggedDamage(shootable, damageType, weapon);
            RefreshUpgradeHue(shootable, damageType, weapon);
        }

        public static int ScaleDamage(HealthChanger changer, IHealthContainer target, int amount)
        {
            if (changer == null || target == null || amount <= 0)
                return amount;

            if (target.IsAlliedToPlayer())
                return amount;

            TaggedPlayerDamage tagged = changer.GetComponent<TaggedPlayerDamage>();
            if (tagged == null)
                tagged = changer.GetComponentInParent<TaggedPlayerDamage>();
            if (tagged == null)
                tagged = changer.GetComponentInChildren<TaggedPlayerDamage>();
            if (tagged == null || string.IsNullOrEmpty(tagged.Type))
                return amount;

            float multiplier = GetDamageMultiplier(tagged.Type);
            if (multiplier <= 0.0f || multiplier == 1.0f)
                return amount;

            int scaled = Mathf.RoundToInt(amount * multiplier);
            return scaled < 1 ? 1 : scaled;
        }

        public static int ScaleIncomingPlayerDamage(IHealthContainer target, int amount)
        {
            if (amount <= 1 || target == null || !target.IsAlliedToPlayer())
                return amount;

            if (!HasSaltsAwarenessUpgrade || !IsSaltsAwarenessActive)
                return amount;

            int scaled = Mathf.CeilToInt(amount * 0.5f);
            return scaled < 1 ? 1 : scaled;
        }

        public static float ScaleSaltsAwarenessDamageMultiplier(float multiplier)
        {
            if (!HasSaltsAwarenessUpgrade || !IsSaltsAwarenessActive)
                return multiplier;

            return multiplier + 1.0f;
        }

        public static bool ShouldPreventForcedSaltsAwarenessEnd(SpendMoneyOverTimeController controller,
            SpendMoneyOverTimeController.EndReason reason)
        {
            PlayerController player = GameAccess.Player;
            return HasSaltsAwarenessUpgrade &&
                   reason == SpendMoneyOverTimeController.EndReason.Forced &&
                   player != null &&
                   object.ReferenceEquals(controller, player.superDandaraController);
        }

        public static bool IsPlayerSuperDandaraController(SpendMoneyOverTimeController controller)
        {
            PlayerController player = GameAccess.Player;
            return player != null && object.ReferenceEquals(controller, player.superDandaraController);
        }

        public static int ScaleSaltsAwarenessCostPerSecond(int baseCost)
        {
            if (!HasSaltsAwarenessUpgrade || baseCost <= 0)
                return baseCost;

            int scaled = Mathf.CeilToInt(baseCost * APSlotSettings.SaltsAwarenessCostMultiplier);
            return scaled < 1 ? 1 : scaled;
        }

        public static void RefreshSaltsAwarenessHue()
        {
            PlayerController player = GameAccess.Player;
            if (player == null)
                return;

            SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
            if (!HasSaltsAwarenessUpgrade || !IsSaltsAwarenessActive)
            {
                RestoreHue(renderers, BaseSaltsAwarenessRendererColors);
                return;
            }

            ApplyHueToRenderers(player.GetComponentsInChildren<SpriteRenderer>(true),
                HueColor("blue"), BaseSaltsAwarenessRendererColors);
        }

        public static void ReapplyAfterPlayerReset()
        {
            PlayerController player = GameAccess.Player;
            if (player == null)
                return;

            if (player.Gun != null)
                player.Gun.ResetState();

            RefreshSaltsAwarenessHue();
        }

        private static float GetDamageMultiplier(string damageType)
        {
            int count = damageType == ArrowProjectileType ? ArrowUpgradeCount : WeaponUpgradeCount;
            if (count <= 0)
                return 1.0f;

            float scale = damageType == ArrowProjectileType
                ? APSlotSettings.ArrowDamageUpgradeScale
                : APSlotSettings.WeaponDamageUpgradeScale;

            return scale * count;
        }

        private static int GetSaltsAwarenessTotalCount()
        {
            return SaveSync.GetReceivedItemCount("Salt's Awareness") +
                   SaveSync.GetReceivedItemCount(SaltsAwarenessUpgradeItem);
        }

        private static bool IsPlayerGun(Gun origin)
        {
            PlayerController player = GameAccess.Player;
            if (player == null || origin == null)
                return false;

            if (object.ReferenceEquals(origin, player.Gun))
                return true;

            Transform originTransform = origin.transform;
            Transform playerTransform = player.transform;
            return originTransform != null && playerTransform != null && originTransform.IsChildOf(playerTransform);
        }

        private static void SetTaggedDamage(Shootable shootable, string damageType, StoryEvent weapon)
        {
            SetTaggedDamage(shootable.gameObject, damageType, weapon);

            HealthChanger[] healthChangers = shootable.GetComponentsInChildren<HealthChanger>(true);
            for (int i = 0; i < healthChangers.Length; i++)
            {
                HealthChanger healthChanger = healthChangers[i];
                if (healthChanger == null)
                    continue;

                SetTaggedDamage(healthChanger.gameObject, damageType, weapon);
            }
        }

        private static void SetTaggedDamage(GameObject gameObject, string damageType, StoryEvent weapon)
        {
            if (gameObject == null)
                return;

            TaggedPlayerDamage tagged = gameObject.GetComponent<TaggedPlayerDamage>();
            if (tagged == null)
                tagged = gameObject.AddComponent<TaggedPlayerDamage>();

            tagged.Type = damageType;
            tagged.Weapon = weapon;
        }

        private static void ClearTaggedDamage(Shootable shootable)
        {
            TaggedPlayerDamage[] tags = shootable.GetComponentsInChildren<TaggedPlayerDamage>(true);
            for (int i = 0; i < tags.Length; i++)
            {
                TaggedPlayerDamage tag = tags[i];
                if (tag != null)
                    tag.Type = "";
            }
        }

        private static string GetCurrentPlayerDamageType(Shootable shootable)
        {
            string objectName = shootable.name == null ? "" : shootable.name;
            if (objectName.IndexOf("Arrow") >= 0 || objectName.IndexOf("DandaraArrow") >= 0)
                return ArrowProjectileType;

            StoryEvent weapon = GetCurrentPlayerWeapon();

            if (IsDamageWeapon(weapon))
                return WeaponProjectileType;

            if (weapon == StoryEvent.None)
                return ArrowProjectileType;

            return "";
        }

        private static StoryEvent GetCurrentPlayerWeapon()
        {
            PlayerController player = GameAccess.Player;
            return player != null && player.Weapons != null
                ? player.Weapons.CurrentEquippedWeaponStoryEvent
                : StoryEvent.None;
        }

        private static bool IsDamageWeapon(StoryEvent weapon)
        {
            return weapon == StoryEvent.Weapon_Missile ||
                   weapon == StoryEvent.Weapon_EnergyBall ||
                   weapon == StoryEvent.Weapon_Remembrance ||
                   weapon == StoryEvent.Weapon_Bounce ||
                   weapon == StoryEvent.Weapon_Boomerang;
        }

        private static void RefreshUpgradeHue(Shootable shootable, string damageType, StoryEvent weapon)
        {
            int count = damageType == ArrowProjectileType ? ArrowUpgradeCount : WeaponUpgradeCount;
            SpriteRenderer[] renderers = shootable.GetComponentsInChildren<SpriteRenderer>(true);
            if (count <= 0)
            {
                RestoreHue(renderers, BaseRendererColors);
                return;
            }

            Color target = UpgradeHueColor(damageType, weapon, count);
            ApplyHueToRenderers(renderers, target, BaseRendererColors);
        }

        private static void ApplyHueToRenderers(SpriteRenderer[] renderers, Color target,
            Dictionary<int, Color> baseColors)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                int id = renderer.GetInstanceID();
                Color baseColor;
                if (!baseColors.TryGetValue(id, out baseColor))
                {
                    baseColor = renderer.color;
                    baseColors[id] = baseColor;
                }

                renderer.color = ReplaceHue(baseColor, target);
            }
        }

        private static void RestoreHue(SpriteRenderer[] renderers, Dictionary<int, Color> baseColors)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                int id = renderer.GetInstanceID();
                Color baseColor;
                if (baseColors.TryGetValue(id, out baseColor))
                    renderer.color = baseColor;
            }
        }

        private static Color UpgradeHueColor(string damageType, StoryEvent weapon, int count)
        {
            if (damageType == ArrowProjectileType)
                return UpgradeColorFromNames(count, "green", "yellow", "blue", "red", "purple");

            switch (weapon)
            {
                case StoryEvent.Weapon_Missile:
                    return UpgradeColorFromNames(count, "green", "yellow", "blue", "red", "purple");
                case StoryEvent.Weapon_Bounce:
                    return UpgradeColorFromNames(count, "purple", "green", "yellow", "red", "blue");
                case StoryEvent.Weapon_EnergyBall:
                    return UpgradeColorFromNames(count, "blue", "green", "yellow", "red", "purple");
                case StoryEvent.Weapon_Boomerang:
                    return UpgradeColorFromNames(count, "red", "green", "yellow", "blue", "purple");
                case StoryEvent.Weapon_Remembrance:
                    return UpgradeColorFromNames(count, "yellow", "green", "blue", "red", "purple");
                default:
                    return UpgradeColorFromNames(count, "green", "yellow", "blue", "red", "purple");
            }
        }

        private static Color UpgradeColorFromNames(int upgradeCount, string baseColor, string upgrade1,
            string upgrade2, string upgrade3, string upgrade4)
        {
            switch (Clamp(upgradeCount, 0, 4))
            {
                case 0:
                    return HueColor(baseColor);
                case 1:
                    return HueColor(upgrade1);
                case 2:
                    return HueColor(upgrade2);
                case 3:
                    return HueColor(upgrade3);
                default:
                    return HueColor(upgrade4);
            }
        }

        private static Color HueColor(string name)
        {
            switch (name)
            {
                case "green":
                    return Color.green;
                case "yellow":
                    return Color.yellow;
                case "blue":
                    return Color.blue;
                case "red":
                    return Color.red;
                case "purple":
                    return new Color(0.70f, 0.20f, 1.00f, 1.0f);
                default:
                    return Color.white;
            }
        }

        private static Color ReplaceHue(Color source, Color target)
        {
            float sourceH;
            float sourceS;
            float sourceV;
            float targetH;
            float targetS;
            float targetV;
            Color.RGBToHSV(source, out sourceH, out sourceS, out sourceV);
            Color.RGBToHSV(target, out targetH, out targetS, out targetV);
            Color result = Color.HSVToRGB(targetH, sourceS, sourceV);
            result.a = source.a;
            return result;
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private class TaggedPlayerDamage : MonoBehaviour
        {
            public string Type = PlayerProjectileTag;
            public StoryEvent Weapon = StoryEvent.None;
        }
    }
}

/*
 * ArchDandara documentation
 * Purpose: Stores per-slot options received from AP, including costs, colors, DeathLink, salt scaling, and goal behavior.
 * Why: Settings are per player, not global, so multiworld and multi-instance sessions stay independent.
 * Notes: All values here should be safe defaults because slot data may be absent when testing against older APWorld builds.
 */

using System.Collections.Generic;
using ArchDandara.Game;
using UnityEngine;

namespace ArchDandara.Archipelago
{
    public static class APSlotSettings
    {
        private static int AmmoManaCostMode;
        private static int GoalTypeMode;
        private static int ShopCostMode;
        private static int SaltDropMultiplierPercent = 100;
        private static int DeathRecoveryPercent = 100;
        private static int ApSaltMultiplierMode;
        private static int ApFearSaltMultiplierMode;
        private static int ArrowDamageUpgradeMax;
        private static int ArrowDamageUpgradeScalePercent = 100;
        private static int WeaponDamageUpgradeMax;
        private static int WeaponDamageUpgradeScalePercent = 100;
        private static bool SaltsAwarenessUpgradeEnabled;
        private static int SaltsAwarenessCostReductionPercent = 40;
        private static bool DeathLinkEnabled;
        private static Color BoughtColor = new Color(0.10f, 0.55f, 1.0f, 1.0f);
        private static Color ReceivedColor = new Color(0.70f, 0.20f, 1.0f, 1.0f);
        private static Color ReceivedOnlyColor = new Color(1.0f, 0.20f, 0.70f, 1.0f);

        public static float AmmoCostMultiplier
        {
            get
            {
                switch (AmmoManaCostMode)
                {
                    case 1:
                        return 0.5f;
                    case 2:
                        return 0.25f;
                    case 3:
                        return 2.0f;
                    default:
                        return 1.0f;
                }
            }
        }

        public static Color ShopBoughtColor
        {
            get { return BoughtColor; }
        }

        public static bool DeathLink
        {
            get { return DeathLinkEnabled; }
        }

        public static bool IsFinalBossGoal
        {
            get { return GoalTypeMode == 0; }
        }

        public static bool IsGoalStoryEvent(StoryEvent storyEvent)
        {
            if (GoalTypeMode == 1)
                return storyEvent == StoryEvent.DLCF_FearEnded;

            return storyEvent == StoryEvent.FinalBoss_Kill;
        }

        public static Color ShopReceivedColor
        {
            get { return ReceivedColor; }
        }

        public static Color ShopReceivedOnlyColor
        {
            get { return ReceivedOnlyColor; }
        }

        public static float ShopCostMultiplier
        {
            get
            {
                switch (ShopCostMode)
                {
                    case 1:
                        return 0.85f;
                    case 2:
                        return 0.75f;
                    case 3:
                        return 0.36f;
                    case 4:
                        return 1.15f;
                    case 5:
                        return 1.20f;
                    case 6:
                        return 1.35f;
                    default:
                        return 1.0f;
                }
            }
        }

        public static float SaltDropMultiplier
        {
            get { return SaltDropMultiplierPercent / 100.0f; }
        }

        public static float DeathRecoveryMultiplier
        {
            get { return DeathRecoveryPercent / 100.0f; }
        }

        public static float ApSaltMultiplier
        {
            get { return ResolveApMoneyMultiplier(ApSaltMultiplierMode, false); }
        }

        public static float ApFearSaltMultiplier
        {
            get { return ResolveApMoneyMultiplier(ApFearSaltMultiplierMode, true); }
        }

        public static int ArrowDamageUpgradeLimit
        {
            get { return ArrowDamageUpgradeMax; }
        }

        public static float ArrowDamageUpgradeScale
        {
            get { return ArrowDamageUpgradeScalePercent / 100.0f; }
        }

        public static int WeaponDamageUpgradeLimit
        {
            get { return WeaponDamageUpgradeMax; }
        }

        public static float WeaponDamageUpgradeScale
        {
            get { return WeaponDamageUpgradeScalePercent / 100.0f; }
        }

        public static bool SaltsAwarenessUpgradeInPool
        {
            get { return SaltsAwarenessUpgradeEnabled; }
        }

        public static float SaltsAwarenessCostMultiplier
        {
            get { return (100 - SaltsAwarenessCostReductionPercent) / 100.0f; }
        }

        public static void ApplySlotData(IDictionary<string, object> slotData)
        {
            if (slotData == null)
                return;

            // Clamp generated values before storing them. This protects the mod from older yaml
            // templates, hand-edited slot settings files, and future APWorld changes.
            GoalTypeMode = GetInt(slotData, "goal_type", 0);
            AmmoManaCostMode = GetInt(slotData, "ammo_mana_cost", 0);
            ShopCostMode = GetInt(slotData, "shop_cost", 0);
            SaltDropMultiplierPercent = Clamp(GetInt(slotData, "salt_drop_multiplier", 100), 0, 800);
            DeathRecoveryPercent = Clamp(GetInt(slotData, "death_recovery_percent", 100), 1, 100);
            ApSaltMultiplierMode = GetInt(slotData, "ap_salt_amount", 100);
            ApFearSaltMultiplierMode = GetInt(slotData, "ap_fear_salt_amount", 100);
            ArrowDamageUpgradeMax = Clamp(GetInt(slotData, "dandara_arrow_damage_upgrade_amount", 0), 0, 4);
            ArrowDamageUpgradeScalePercent = Clamp(GetInt(slotData, "dandara_arrow_damage_upgrade_scale", 100), 50, 300);
            WeaponDamageUpgradeMax = Clamp(GetInt(slotData, "dandara_weapon_damage_upgrade_amount", 0), 0, 4);
            WeaponDamageUpgradeScalePercent = Clamp(GetInt(slotData, "dandara_weapon_damage_upgrade_scale", 100), 50, 300);
            SaltsAwarenessUpgradeEnabled = GetInt(slotData, "salts_awareness_upgrade", 0) != 0;
            SaltsAwarenessCostReductionPercent = Clamp(GetInt(slotData, "salts_awareness_cost_reduction", 40), 5, 75);
            DeathLinkEnabled = GetInt(slotData, "death_link", 0) != 0;
            BoughtColor = ResolveColor(slotData, "bought_color", "custom_bought_color", BoughtColor, 0);
            ReceivedColor = ResolveColor(slotData, "received_color", "custom_received_color", ReceivedColor, 1);
            ReceivedOnlyColor = ResolveColor(slotData, "received_only_color", "custom_received_only_color",
                ReceivedOnlyColor, 2);

            // Ammo costs are cached per weapon component. Reset after settings change so the next
            // weapon refresh recalculates from vanilla base values instead of stale multipliers.
            AmmoCostService.ResetCache();
            MLLog.Msg("[APSettings] Applied slot settings: goal_type=" + GoalTypeMode +
                            " ammo_mana_cost=" + AmmoManaCostMode +
                            " multiplier=" + AmmoCostMultiplier +
                            " shop_cost=" + ShopCostMode +
                            " shop_multiplier=" + ShopCostMultiplier +
                            " salt_drop_multiplier=" + SaltDropMultiplier +
                            " death_recovery=" + DeathRecoveryMultiplier +
                            " ap_salt=" + ApSaltMultiplier +
                            " ap_fear_salt=" + ApFearSaltMultiplier +
                            " arrow_damage_upgrades=" + ArrowDamageUpgradeMax +
                            " arrow_damage_scale=" + ArrowDamageUpgradeScale +
                            " weapon_damage_upgrades=" + WeaponDamageUpgradeMax +
                            " weapon_damage_scale=" + WeaponDamageUpgradeScale +
                            " salts_awareness_upgrade=" + SaltsAwarenessUpgradeEnabled +
                            " salts_awareness_cost_multiplier=" + SaltsAwarenessCostMultiplier +
                            " death_link=" + DeathLinkEnabled);
        }

        public static int ScaleApMoney(string itemName, int amount)
        {
            if (amount <= 0)
                return amount;

            // AP salt item scaling is separate from world drop scaling. This prevents local drop
            // multipliers from changing item values sent by other players.
            float multiplier = itemName == "Pleas of the Salt Fear" ? ApFearSaltMultiplier : ApSaltMultiplier;
            int result = (int)(amount * multiplier + 0.5f);
            return result < 0 ? 0 : result;
        }

        public static int ScaleDeathRecoveryMoney(int amount)
        {
            if (amount <= 0)
                return 0;

            int result = (int)(amount * DeathRecoveryMultiplier + 0.5f);
            return result < 0 ? 0 : result;
        }

        private static float ResolveApMoneyMultiplier(int mode, bool fear)
        {
            if (mode > 3)
                return mode / 100.0f;

            if (fear)
            {
                switch (mode)
                {
                    case 1:
                        return 0.5f;
                    case 2:
                        return 1.5f;
                    case 3:
                        return 3.0f;
                    default:
                        return 1.0f;
                }
            }

            switch (mode)
            {
                case 1:
                    return 0.5f;
                case 2:
                    return 2.0f;
                case 3:
                    return 5.0f;
                default:
                    return 1.0f;
            }
        }

        private static Color ResolveColor(IDictionary<string, object> slotData, string optionKey, string customPrefix,
            Color fallback, int colorRole)
        {
            int scheme = GetInt(slotData, optionKey, 0);
            if (scheme == 5)
            {
                // Custom colors are only present in slot_data when the APWorld selected Custom for
                // that specific bar role, so fall back per channel if older data is missing one.
                return FromRgb(GetInt(slotData, customPrefix + "_r", ToByte(fallback.r)),
                    GetInt(slotData, customPrefix + "_g", ToByte(fallback.g)),
                    GetInt(slotData, customPrefix + "_b", ToByte(fallback.b)));
            }

            return PresetColor(scheme, colorRole, fallback);
        }

        private static Color PresetColor(int scheme, int colorRole, Color fallback)
        {
            switch (scheme)
            {
                case 0:
                    return colorRole == 0 ? new Color(0.10f, 0.55f, 1.0f, 1.0f) :
                        colorRole == 1 ? new Color(0.70f, 0.20f, 1.0f, 1.0f) :
                        new Color(1.0f, 0.20f, 0.70f, 1.0f);
                case 1:
                    return colorRole == 0 ? FromRgb(0, 114, 178) :
                        colorRole == 1 ? FromRgb(213, 94, 0) :
                        FromRgb(204, 121, 167);
                case 2:
                    return colorRole == 0 ? FromRgb(230, 159, 0) :
                        colorRole == 1 ? FromRgb(86, 180, 233) :
                        FromRgb(240, 228, 66);
                case 3:
                    return colorRole == 0 ? FromRgb(0, 158, 115) :
                        colorRole == 1 ? FromRgb(204, 121, 167) :
                        FromRgb(230, 159, 0);
                case 4:
                    return colorRole == 0 ? FromRgb(255, 255, 255) :
                        colorRole == 1 ? FromRgb(170, 85, 255) :
                        FromRgb(255, 210, 0);
                default:
                    return fallback;
            }
        }

        private static Color FromRgb(int r, int g, int b)
        {
            return new Color(ClampByte(r) / 255.0f, ClampByte(g) / 255.0f, ClampByte(b) / 255.0f, 1.0f);
        }

        private static int ClampByte(int value)
        {
            return Clamp(value, 0, 255);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        private static int ToByte(float value)
        {
            return ClampByte((int)(value * 255.0f));
        }

        private static int GetInt(IDictionary<string, object> slotData, string key, int defaultValue)
        {
            object value;
            if (!slotData.TryGetValue(key, out value) || value == null)
                return defaultValue;

            if (value is int)
                return (int)value;
            if (value is long)
                return (int)(long)value;
            if (value is short)
                return (short)value;
            if (value is byte)
                return (byte)value;

            int parsed;
            return int.TryParse(value.ToString(), out parsed) ? parsed : defaultValue;
        }
    }
}

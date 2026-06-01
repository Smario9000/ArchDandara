/*
 * ArchDandara documentation
 * Purpose: Maps AP item names to Dandara StoryEvents and money amounts.
 * Why: The APWorld uses player-facing item names while the game uses StoryEvent ids, so this is the translation table.
 * Notes: When adding an AP item, update this file and the APWorld item pool together so generation and runtime stay aligned.
 */

using System.Collections.Generic;

namespace ArchDandara.Game
{
    public static class ItemIds
    {
        private static readonly Dictionary<string, StoryEvent> StoryEventByItemName =
            new Dictionary<string, StoryEvent>();

        private static readonly Dictionary<string, int> MoneyByItemName = new Dictionary<string, int>();

        public static void Initialize()
        {
            StoryEventByItemName.Clear();
            MoneyByItemName.Clear();

            StoryEventByItemName["FearKey"] = StoryEvent.DLCF_EntranceAccessUnlocked;
            StoryEventByItemName["FreeNara"] = StoryEvent.DLCF_FreedNara;
            StoryEventByItemName["TimeFlag"] = StoryEvent.DLCF_TimeFlagTrap;
            StoryEventByItemName["Stone of Creation"] = StoryEvent.PU_Stone_Creation;
            StoryEventByItemName["Rock of Remembrance"] = StoryEvent.PU_Stone_Remembrance;
            StoryEventByItemName["Stone of Intention"] = StoryEvent.PU_Stone_Intention;
            StoryEventByItemName["Pearl of Dreams"] = StoryEvent.PU_Stone_Dreams;
            StoryEventByItemName["Shell Mirror"] = StoryEvent.PU_DLCF_FinalKey;
            StoryEventByItemName["Heart of the Great Salt"] = StoryEvent.PU_Health;
            StoryEventByItemName["Scarf of Freedom"] = StoryEvent.PU_Ammo;
            StoryEventByItemName["Essence of Salt"] = StoryEvent.PU_HealthFlask;
            StoryEventByItemName["Infusion of Salt"] = StoryEvent.PU_ManaFlask;
            StoryEventByItemName["Essence of Salt Enhancer"] = StoryEvent.PU_HealthFlaskUpgrade;
            StoryEventByItemName["Infusion of Salt Enhancer"] = StoryEvent.PU_ManaFlaskUpgrade;
            StoryEventByItemName["Jonny B. Missiles"] = StoryEvent.Weapon_Missile;
            StoryEventByItemName["Anxiety Shock"] = StoryEvent.Weapon_EnergyBall;
            StoryEventByItemName["Memories Shaft"] = StoryEvent.Weapon_Remembrance;
            StoryEventByItemName["Logic Blast"] = StoryEvent.Weapon_Bounce;
            StoryEventByItemName["Skin Knitter"] = StoryEvent.Weapon_WaterBomb;
            StoryEventByItemName["Displaced Presence"] = StoryEvent.Weapon_Teleport;
            StoryEventByItemName["Paint Platform"] = StoryEvent.D_Painter;
            StoryEventByItemName["Music Platform"] = StoryEvent.D_Musician;
            StoryEventByItemName["DLC StoryEvent 1"] = StoryEvent.D_DLCF_MastersIntroduction;
            StoryEventByItemName["DLC StoryEvent 2"] = StoryEvent.D_DLCF_ExplorerStart;
            StoryEventByItemName["DLC StoryEvent 3"] = StoryEvent.D_DLCF_NobleStart;
            StoryEventByItemName["DLC StoryEvent 4"] = StoryEvent.D_DLCF_PersistentStart;
            StoryEventByItemName["DLC StoryEvent 5"] = StoryEvent.D_DLCF_ExplorerFinish;
            StoryEventByItemName["DLC StoryEvent 6"] = StoryEvent.D_DLCF_NobleFinish;
            StoryEventByItemName["DLC StoryEvent 7"] = StoryEvent.D_DLCF_PersistentFinish;
            StoryEventByItemName["Map"] = StoryEvent.PU_Map;
            StoryEventByItemName["Bracers of the Patient"] = StoryEvent.PU_Shield;
            StoryEventByItemName["Arrow of Freedom"] = StoryEvent.PU_DandaraArrow;
            StoryEventByItemName["Salt's Awareness"] = StoryEvent.PU_SuperDandara;
            StoryEventByItemName["FinalBoss_Kill"] = StoryEvent.FinalBoss_Kill;

            MoneyByItemName["Pleas of the Salt Fear"] = 3000;
            MoneyByItemName["Pleas of the Salt"] = 1000;
            MoneyByItemName["Salt"] = 100;
            MoneyByItemName["Salt 100"] = 100;
            MoneyByItemName["Salt 250"] = 250;
            MoneyByItemName["Salt 500"] = 500;
            MoneyByItemName["Salt 1000"] = 1000;
        }

        public static bool TryGetStoryEvent(string itemName, out StoryEvent storyEvent)
        {
            return StoryEventByItemName.TryGetValue(itemName, out storyEvent);
        }

        public static bool TryGetItemNameForStoryEvent(StoryEvent storyEvent, out string itemName)
        {
            foreach (KeyValuePair<string, StoryEvent> pair in StoryEventByItemName)
            {
                if (pair.Value == storyEvent)
                {
                    itemName = pair.Key;
                    return true;
                }
            }

            itemName = null;
            return false;
        }

        public static bool IsDlcTrialGateStoryEvent(StoryEvent storyEvent)
        {
            return storyEvent == StoryEvent.D_DLCF_MastersIntroduction ||
                   storyEvent == StoryEvent.D_DLCF_ExplorerStart ||
                   storyEvent == StoryEvent.D_DLCF_NobleStart ||
                   storyEvent == StoryEvent.D_DLCF_PersistentStart ||
                   storyEvent == StoryEvent.D_DLCF_ExplorerFinish ||
                   storyEvent == StoryEvent.D_DLCF_NobleFinish ||
                   storyEvent == StoryEvent.D_DLCF_PersistentFinish ||
                   storyEvent == StoryEvent.DLCF_TimeFlagTrap;
        }

        public static bool TryGetMoneyAmount(string itemName, out int amount)
        {
            return MoneyByItemName.TryGetValue(itemName, out amount);
        }

        public static bool IsMoneyItem(string itemName)
        {
            return !string.IsNullOrEmpty(itemName) && MoneyByItemName.ContainsKey(itemName);
        }

        public static bool IsUpgradeCounterItem(string itemName)
        {
            return itemName == DamageUpgradeService.ArrowDamageUpgradeItem ||
                   itemName == DamageUpgradeService.WeaponDamageUpgradeItem ||
                   itemName == DamageUpgradeService.SaltsAwarenessUpgradeItem;
        }

        public static bool IsVanillaStoryRewardBlocked(StoryEvent storyEvent)
        {
            return storyEvent == StoryEvent.D_Painter ||
                   storyEvent == StoryEvent.D_Musician ||
                   storyEvent == StoryEvent.DLCF_FreedNara ||
                   storyEvent == StoryEvent.PU_Stone_Creation ||
                   storyEvent == StoryEvent.PU_Stone_Dreams ||
                   IsDlcTrialGateStoryEvent(storyEvent);
        }
    }
}

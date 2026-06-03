/*
 * ArchDandara documentation
 * Purpose: Converts received AP item names into game effects, story events, money, permits, upgrades, and world-object changes.
 * Why: All item behavior flows through one grant layer so delayed grants, duplicate handling, and vanilla-block exceptions stay consistent.
 * Notes: Grant methods return false only when the item should be retried later; return true for handled-but-no-op cases.
 */

namespace ArchDandara.Game
{
    public static class ItemGrantService
    {
        public static bool Grant(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return false;

            MLLog.Msg("[ItemGrant] Granting AP item: " + itemName);

            // Handle non-StoryEvent items first. These change mod state or money directly and
            // should not fall through into the generic StoryEvent table.
            if (TryGrantBossRoomKey(itemName))
                return true;

            if (TryGrantMoney(itemName))
                return true;

            if (ItemIds.IsMoneyItem(itemName))
                return false;

            if (TryConvertDuplicateReceivedItem(itemName))
                return true;

            if (TryGrantShopPermit(itemName))
                return true;

            // Upgrade counters are AP-only progression. The game has no vanilla pickup object
            // for these, so SaveSync tracks counts and the relevant services apply the effect.
            if (ItemIds.IsUpgradeCounterItem(itemName))
            {
                if (itemName == DamageUpgradeService.SaltsAwarenessUpgradeItem &&
                    !DamageUpgradeService.HasAnySaltsAwarenessItem)
                {
                    MLLog.Msg("[ItemGrant] Salt's Awareness upgrade received first; applying base Salt's Awareness.");
                    return GrantStoryEventItem(StoryEvent.PU_SuperDandara);
                }

                MLLog.Msg("[ItemGrant] Applied AP upgrade counter item: " + itemName);
                return true;
            }

            if (WorldObjectGrantService.TryGrant(itemName))
                return true;

            StoryEvent storyEvent;
            if (ItemIds.TryGetStoryEvent(itemName, out storyEvent))
            {
                MLLog.Msg("[ItemGrant] Applying StoryEvent: " + storyEvent);
                return GrantStoryEventItem(storyEvent);
            }

            MLLog.Warning("[ItemGrant] Unmapped AP item: " + itemName);
            return true;
        }

        private static bool TryConvertDuplicateReceivedItem(string itemName)
        {
            if (TryConvertFinalBossOnlyItem(itemName))
                return true;

            if (IsTrueEndingChanceItem(itemName) && HasReceivedDuplicate(itemName, 1))
                return ConvertDuplicateToMoney(itemName, "Pleas of the Salt Fear");

            if (itemName == "Arrow of Freedom" && SaveSync.GetReceivedItemCount(itemName) >= 3)
                return ConvertDuplicateToMoney(itemName, "Pleas of the Salt");

            if (IsOneOwnedNormalChanceItem(itemName) && HasReceivedDuplicate(itemName, 1))
                return ConvertDuplicateToMoney(itemName, "Pleas of the Salt");

            if (IsShopPermitChanceItem(itemName) &&
                (SaveSync.HasShopPermit(itemName) || SaveSync.GetReceivedItemCount(itemName) >= 1))
                return ConvertDuplicateToMoney(itemName, "Pleas of the Salt");

            return false;
        }

        private static bool TryConvertFinalBossOnlyItem(string itemName)
        {
            if (itemName != "FreeNara")
                return false;

            if (!ArchDandara.Archipelago.APSlotSettings.IsFinalBossGoal)
                return false;

            return ConvertDuplicateToMoney(itemName, "Pleas of the Salt Fear");
        }

        private static bool HasReceivedDuplicate(string itemName, int usableCount)
        {
            if (SaveSync.GetReceivedItemCount(itemName) >= usableCount)
                return true;

            StoryManager storyManager = GameAccess.StoryManager;
            if (object.ReferenceEquals(storyManager, null))
                return false;

            StoryEvent eventId;
            if (!ItemIds.TryGetStoryEvent(itemName, out eventId))
                return false;

            return storyManager.GetEvent(eventId);
        }

        private static bool ConvertDuplicateToMoney(string itemName, string moneyItemName)
        {
            MLLog.Msg("[ItemGrant] Converted duplicate AP item to " + moneyItemName + ": " + itemName);
            return TryGrantMoney(moneyItemName);
        }

        private static bool IsOneOwnedNormalChanceItem(string itemName)
        {
            return itemName == "Stone of Creation" ||
                   itemName == "Rock of Remembrance" ||
                   itemName == "Stone of Intention" ||
                   itemName == "Pearl of Dreams" ||
                   itemName == "Jonny B. Missiles" ||
                   itemName == "Anxiety Shock" ||
                   itemName == "Memories Shaft" ||
                   itemName == "Logic Blast" ||
                   itemName == "Skin Knitter" ||
                   itemName == "Paint Platform" ||
                   itemName == "Music Platform" ||
                   itemName == "Map" ||
                   itemName == "Bracers of the Patient" ||
                   itemName == "Salt's Awareness";
        }

        private static bool IsTrueEndingChanceItem(string itemName)
        {
            return itemName == "Shell Mirror" ||
                   itemName == "FearKey" ||
                   itemName == "FreeNara";
        }

        private static bool IsShopPermitChanceItem(string itemName)
        {
            return itemName == "Essence Enhancer Permit" ||
                   itemName == "Infusion Enhancer Permit" ||
                   itemName == "Freedom Enhancer Permit" ||
                   itemName == "Heart Enhancer Permit";
        }

        private static bool TryGrantBossRoomKey(string itemName)
        {
            if (itemName != "Boss StoryEvent Key 1" &&
                itemName != "Boss StoryEvent Key 2")
                return false;

            MLLog.Msg("[ItemGrant] Boss room interaction key received: " + itemName);
            return true;
        }

        private static bool TryGrantShopPermit(string itemName)
        {
            if (itemName != "Essence Enhancer Permit" &&
                itemName != "Infusion Enhancer Permit" &&
                itemName != "Freedom Enhancer Permit" &&
                itemName != "Heart Enhancer Permit")
                return false;

            SaveSync.MarkShopPermit(itemName);
            SaveSync.Save();
            UnlockShopPermitEvent(itemName);
            MLLog.Msg("[ItemGrant] Shop permit unlocked: " + itemName);
            return true;
        }

        private static void UnlockShopPermitEvent(string itemName)
        {
            StoryEvent eventId;
            if (!TryGetShopPermitStoryEvent(itemName, out eventId))
                return;

            StoryManager storyManager = GameAccess.StoryManager;
            if (object.ReferenceEquals(storyManager, null))
                return;

            GrantContext.IsArchipelagoGrant = true;
            try
            {
                storyManager.UnlockEvent(eventId);
            }
            finally
            {
                GrantContext.IsArchipelagoGrant = false;
            }
        }

        private static bool TryGetShopPermitStoryEvent(string itemName, out StoryEvent eventId)
        {
            if (itemName == "Essence Enhancer Permit")
            {
                eventId = StoryEvent.PU_HealthFlaskUpgrade;
                return true;
            }

            if (itemName == "Infusion Enhancer Permit")
            {
                eventId = StoryEvent.PU_ManaFlaskUpgrade;
                return true;
            }

            if (itemName == "Freedom Enhancer Permit")
            {
                eventId = StoryEvent.PU_Ammo;
                return true;
            }

            if (itemName == "Heart Enhancer Permit")
            {
                eventId = StoryEvent.PU_Health;
                return true;
            }

            eventId = StoryEvent.None;
            return false;
        }

        private static bool TryGrantMoney(string itemName)
        {
            int amount;
            if (!ItemIds.TryGetMoneyAmount(itemName, out amount))
                return false;

            amount = ArchDandara.Archipelago.APSlotSettings.ScaleApMoney(itemName, amount);

            PlayerController player = GameAccess.Player;
            if (player == null)
            {
                MLLog.Warning("[ItemGrant] Player is null, cannot grant money yet.");
                return false;
            }

            GrantContext.IsArchipelagoGrant = true;
            try
            {
                player.AddMoney(amount);
            }
            finally
            {
                GrantContext.IsArchipelagoGrant = false;
            }

            return true;
        }

        private static bool GrantStoryEventItem(StoryEvent eventId)
        {
            PowerupManager powerupManager = GameAccess.PowerupManager;
            StoryManager storyManager = GameAccess.StoryManager;

            if (powerupManager == null && storyManager == null)
            {
                MLLog.Warning("[ItemGrant] Game managers are not ready, cannot grant " + eventId + " yet.");
                return false;
            }

            GrantContext.IsArchipelagoGrant = true;
            try
            {
                if (powerupManager != null)
                {
                    // Prefer PowerupManager when possible because Powerup.Unlock runs the same
                    // side effects vanilla expects, including counts and on-unlock UnityEvents.
                    Powerup powerup = powerupManager.GetPowerup(eventId);
                    if (powerup != null)
                    {
                        bool powerupUnlocked = powerupManager.TryUnlockWithoutShow(powerup);
                        bool storyUnlocked = EnsureStoryEventUnlocked(storyManager, eventId);
                        MLLog.Msg("[ItemGrant] Powerup grant result: " + eventId +
                                        " | powerupUnlocked=" + powerupUnlocked +
                                        " | storyUnlocked=" + storyUnlocked);

                        if (IsWeaponEvent(eventId) && !TryAddWeapon(eventId))
                        {
                            MLLog.Warning("[ItemGrant] Weapon grant waiting for player weapon system: " +
                                                eventId);
                            return false;
                        }

                        TryBootstrapAmmoHud(eventId);
                        TryRefreshShield(eventId);
                        RefreshPlayerState();
                        HudRefreshService.RefreshAfterWeaponGrant(eventId);
                        return true;
                    }
                }

                if (storyManager != null)
                {
                    // Some events do not have Powerup objects. Those still need the StoryEvent so
                    // game systems and logic checks can see the AP item as owned.
                    EnsureStoryEventUnlocked(storyManager, eventId);
                    if (IsWeaponEvent(eventId) && !TryAddWeapon(eventId))
                    {
                        MLLog.Warning("[ItemGrant] Weapon story grant waiting for player weapon system: " +
                                            eventId);
                        return false;
                    }

                    TryBootstrapAmmoHud(eventId);
                    TryRefreshShield(eventId);
                    RefreshPlayerState();
                    HudRefreshService.RefreshAfterWeaponGrant(eventId);
                    return true;
                }
            }
            finally
            {
                GrantContext.IsArchipelagoGrant = false;
            }

            MLLog.Warning("[ItemGrant] Could not apply StoryEvent item: " + eventId);
            return false;
        }

        private static bool EnsureStoryEventUnlocked(StoryManager storyManager, StoryEvent eventId)
        {
            if (object.ReferenceEquals(storyManager, null) || eventId == StoryEvent.None)
                return false;

            if (storyManager.GetEvent(eventId))
                return true;

            storyManager.UnlockEvent(eventId);
            return storyManager.GetEvent(eventId);
        }

        private static bool TryAddWeapon(StoryEvent eventId)
        {
            PlayerController player = GameAccess.Player;
            if (player == null || player.Weapons == null)
                return false;

            player.Weapons.AddNewWeapon(eventId);
            MLLog.Msg("[ItemGrant] Player weapon add attempted: " + eventId +
                            " | equipped=" + player.Weapons.CurrentEquippedWeaponStoryEvent);
            return player.Weapons.CurrentEquippedWeaponStoryEvent == eventId;
        }

        private static bool IsWeaponEvent(StoryEvent eventId)
        {
            return eventId == StoryEvent.Weapon_Missile ||
                   eventId == StoryEvent.Weapon_EnergyBall ||
                   eventId == StoryEvent.Weapon_Remembrance ||
                   eventId == StoryEvent.Weapon_Bounce ||
                   eventId == StoryEvent.Weapon_Boomerang ||
                   eventId == StoryEvent.Weapon_Vaccum ||
                   eventId == StoryEvent.Weapon_WaterBomb ||
                   eventId == StoryEvent.Weapon_Teleport ||
                   eventId == StoryEvent.Weapon_Firewall;
        }

        private static void TryRefreshShield(StoryEvent eventId)
        {
            if (eventId != StoryEvent.PU_Shield)
                return;

            PlayerController player = GameAccess.Player;
            StoryManager storyManager = GameAccess.StoryManager;
            if (object.ReferenceEquals(player, null) || object.ReferenceEquals(storyManager, null))
                return;

            StoryEvent shieldEvent = player.eventNeededForShield;
            if (shieldEvent == StoryEvent.None)
                shieldEvent = StoryEvent.PU_Shield;

            if (!storyManager.GetEvent(shieldEvent) && !storyManager.GetEvent(StoryEvent.PU_Shield))
                return;

            ShieldController shield = TryGetPlayerShield(player);
            if (object.ReferenceEquals(shield, null))
            {
                MLLog.Warning("[ItemGrant] Shield event applied, but player shield component was not found.");
                return;
            }

            shield.enabled = true;
            // Vanilla shield assumes a special weapon exists because HasAmmo checks for one.
            // AP can grant shield first, so seed ammo and let ShieldPatches relax that condition.
            TryEnsureShieldAmmo(player);
            MLLog.Msg("[ItemGrant] Shield enabled for AP item.");
        }

        private static void TryEnsureShieldAmmo(PlayerController player)
        {
            if (object.ReferenceEquals(player, null) || object.ReferenceEquals(player.Gun, null))
                return;

            AmmoGun gun = player.Gun;
            if (gun.maxAmmo > 0f && gun.ammo > 0f)
                return;

            float minimumMaxAmmo = gun.maxAmmo > 0f ? gun.maxAmmo : gun.ammoPerIncrease;
            if (minimumMaxAmmo <= 0f)
                minimumMaxAmmo = 4f;

            gun.SetState(minimumMaxAmmo, minimumMaxAmmo);
            MLLog.Msg("[ItemGrant] Seeded shield ammo state: ammo=" + gun.ammo + " max=" + gun.maxAmmo);
        }

        private static ShieldController TryGetPlayerShield(PlayerController player)
        {
            try
            {
                System.Reflection.FieldInfo field = typeof(PlayerController).GetField("_shield",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (!object.ReferenceEquals(field, null))
                {
                    object value = field.GetValue(player);
                    ShieldController shield = value as ShieldController;
                    if (!object.ReferenceEquals(shield, null))
                        return shield;
                }
            }
            catch (System.Exception ex)
            {
                MLLog.Warning("[ItemGrant] Failed to read player shield field: " +
                              ex.GetType().Name + ": " + ex.Message);
            }

            try
            {
                return player.GetComponentInChildren<ShieldController>();
            }
            catch
            {
                return null;
            }
        }

        private static void TryBootstrapAmmoHud(StoryEvent grantedEvent)
        {
            if (!HudRefreshService.ShouldShowAmmoHud(grantedEvent) || SaveSync.HasAmmoHudBootstrap())
                return;

            PlayerController player = GameAccess.Player;
            if (player == null || player.Weapons == null)
                return;

            // The ammo HUD only appears after the game has seen a weapon-style unlock. A temporary
            // missile add wakes the HUD up, then we restore the real current weapon state.
            StoryEvent restoreEvent = player.Weapons.CurrentEquippedWeaponStoryEvent;
            player.Weapons.AddNewWeapon(StoryEvent.Weapon_Missile);

            if (restoreEvent != StoryEvent.None && restoreEvent != StoryEvent.Weapon_Missile)
                player.Weapons.AddNewWeapon(restoreEvent);
            else if (grantedEvent == StoryEvent.PU_Shield)
                player.Weapons.RemoveWeapon(false);

            SaveSync.MarkAmmoHudBootstrap();
            SaveSync.Save();
            HudRefreshService.ForceAmmoHud();
            MLLog.Msg("[ItemGrant] Applied one-time fake Jonny B. Missiles ammo HUD bootstrap for " +
                            grantedEvent);
        }

        private static void RefreshPlayerState()
        {
            PlayerController player = GameAccess.Player;
            if (player != null && player.Gun != null)
                player.Gun.ResetState();
        }

    }
}

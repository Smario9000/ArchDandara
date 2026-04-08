using System;
using MelonLoader;
using UnityEngine;

namespace ArchDandara.Gamehook
{
    public static class DebugItemMenu
    {
        private static bool _visible;
        private static Rect _windowRect = new Rect(20f, 20f, 420f, 520f);
        private static Vector2 _scrollPos = Vector2.zero;

        // Replace these with the real StoryEvent names once you confirm them.
        private static readonly StoryEvent[] TestPowerups =
        {
            StoryEvent.PU_Map,
            StoryEvent.PU_DandaraArrow,
            StoryEvent.PU_HealthFlask,
            StoryEvent.PU_ManaFlask
        };

        // Fill these in with the actual move/weapon StoryEvents once you confirm them.
        private static readonly StoryEvent[] TestMoves =
        {
            // Example placeholders:
            // StoryEvent.MusicianPlatform,
            // StoryEvent.PainterPlatform
        };

        public static void OnUpdate()
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // Ctrl + Shift + F8 toggles the debug item window.
            if (ctrl && shift && Input.GetKeyDown(KeyCode.F8))
            {
                _visible = !_visible;
                MelonLogger.Msg("[DebugItemMenu] " + (_visible ? "Opened" : "Closed"));
            }
        }

        public static void OnGUI()
        {
            if (!_visible)
                return;

            _windowRect = GUI.Window(94821, _windowRect, DrawWindow, "ArchDandara Debug Item Menu");
        }

        private static void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Testing helpers for powerups, moves, and upgrades.");

            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Width(400f), GUILayout.Height(440f));

            DrawSectionLabel("Quick Actions");

            if (GUILayout.Button("Give 100 Money"))
                GiveMoney(100);

            if (GUILayout.Button("Give 1000 Money"))
                GiveMoney(1000);

            if (GUILayout.Button("HealPlayer"))
                HealPlayer();

            GUILayout.Space(10f);
            DrawSectionLabel("Powerups");

            for (int i = 0; i < TestPowerups.Length; i++)
            {
                StoryEvent reward = TestPowerups[i];
                if (GUILayout.Button("Give Powerup: " + reward))
                    GivePowerupFromStoryEvent(reward);
            }

            GUILayout.Space(10f);
            DrawSectionLabel("Moves / Weapons");

            if (TestMoves.Length == 0)
            {
                GUILayout.Label("Add the real StoryEvent names for MusicianPlatform / PainterPlatform here.");
            }
            else
            {
                for (int i = 0; i < TestMoves.Length; i++)
                {
                    StoryEvent moveEvent = TestMoves[i];
                    if (GUILayout.Button("Give Move: " + moveEvent))
                        GiveWeaponLikeMove(moveEvent);
                }
            }

            GUILayout.Space(10f);
            DrawSectionLabel("Shop / Upgrade State");

            if (GUILayout.Button("Buy One Shop Level (NYI)"))
                GiveShopLevel();

            if (GUILayout.Button("Max Shop Levels (NYI)"))
                MaxShopLevels();

            GUILayout.Space(10f);
            DrawSectionLabel("Raw Story Events");

            if (GUILayout.Button("Unlock HUD_Unlock"))
                UnlockStoryEvent(StoryEvent.HUD_Unlock);

            if (GUILayout.Button("Unlock Started"))
                UnlockStoryEvent(StoryEvent.Started);

            GUILayout.EndScrollView();

            if (GUILayout.Button("Close"))
                _visible = false;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private static void DrawSectionLabel(string text)
        {
            GUILayout.Space(4f);
            GUILayout.Label("---- " + text + " ----");
        }

        private static void GiveMoney(int amount)
        {
            try
            {
                PlayerController player = GetPlayer();
                if (player == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] Player was null");
                    return;
                }
                int before = player.state.currentMoney;

                MoneyGrantContext.AllowNextMoneyGrant = true;
                player.AddMoney(amount);

                int after = player.state.currentMoney;

                MelonLogger.Msg("[DebugItemMenu] Gave money: " + amount + " | Before=" + before + " After=" + after);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DebugItemMenu] GiveMoney failed: " + ex);
            }
        }

        private static void HealPlayer()
        {
            try
            {
                PlayerController player = GetPlayer();
                if (player == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] Player was null");
                    return;
                }

                // These two powerups are the cleanest current test path for health/mana flask related state.
                PowerupManager manager = PersistentSingleton<PowerupManager>.instance;
                if (manager != null)
                {
                    Powerup health = manager.GetPowerup(StoryEvent.PU_HealthFlask);
                    Powerup mana = manager.GetPowerup(StoryEvent.PU_ManaFlask);

                    if (health != null)
                        manager.TryUnlockWithoutShow(health);

                    if (mana != null)
                        manager.TryUnlockWithoutShow(mana);
                }

                if (player.Gun != null)
                    player.Gun.ResetState();

                MelonLogger.Msg("[DebugItemMenu] Heal/refresh applied");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DebugItemMenu] HealPlayer failed: " + ex);
            }
        }

        private static void GivePowerupFromStoryEvent(StoryEvent eventId)
        {
            try
            {
                PowerupManager manager = PersistentSingleton<PowerupManager>.instance;
                if (manager == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] PowerupManager was null");
                    return;
                }

                Powerup powerup = manager.GetPowerup(eventId);
                if (powerup == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] No powerup found for " + eventId);
                    return;
                }

                manager.TryUnlockWithoutShow(powerup);
                MelonLogger.Msg("[DebugItemMenu] Gave powerup: " + eventId);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DebugItemMenu] GivePowerupFromStoryEvent failed: " + ex);
            }
        }

        private static void GiveWeaponLikeMove(StoryEvent eventId)
        {
            try
            {
                StoryManager story = PersistentSingleton<StoryManager>.instance;
                PlayerController player = GetPlayer();

                if (story == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] StoryManager was null");
                    return;
                }

                if (player == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] Player was null");
                    return;
                }

                story.UnlockEvent(eventId);

                if (player.Weapons != null)
                    player.Weapons.AddNewWeapon(eventId);

                if (player.Gun != null)
                    player.Gun.ResetState();

                MelonLogger.Msg("[DebugItemMenu] Gave move/weapon: " + eventId);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DebugItemMenu] GiveWeaponLikeMove failed: " + ex);
            }
        }

        private static void UnlockStoryEvent(StoryEvent eventId)
        {
            try
            {
                StoryManager story = PersistentSingleton<StoryManager>.instance;
                if (story == null)
                {
                    MelonLogger.Warning("[DebugItemMenu] StoryManager was null");
                    return;
                }

                bool result = story.UnlockEvent(eventId);
                MelonLogger.Msg("[DebugItemMenu] UnlockStoryEvent " + eventId + " => " + result);
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[DebugItemMenu] UnlockStoryEvent failed: " + ex);
            }
        }

        private static void GiveShopLevel()
        {
            MelonLogger.Warning("[DebugItemMenu] GiveShopLevel is not implemented yet. PowerupManager has no GetCurrentPowerup() in this game build.");
        }

        private static void MaxShopLevels()
        {
            MelonLogger.Warning("[DebugItemMenu] MaxShopLevels is not implemented yet. PowerupManager has no GetCurrentPowerup() in this game build.");
        }

        private static PlayerController GetPlayer()
        {
            GameManager gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetPlayer() : null;
        }
    }
}
// GreatRuinsTutorialSwitchLogic.cs
// Automatically turns on the two Great Ruins tutorial levers
// when the room loads, so the player does not need to solve that tutorial again.

using System;
using HarmonyLib;
using MelonLoader;
using UnityEngine;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(GameManager), "OnTransitionEnded")]
    public static class GreatRuinsTutorialSwitchLogic
    {
        // Prevents this logic from running over and over in the same session.
        private static bool _attemptedThisSession;

        // The room where the tutorial levers exist.
        private const string TargetScene = "A1_GreatRuins";

        private static void Postfix()
        {
            try
            {
                var gm = PersistentSingleton<GameManager>.instance;
                if ((object)gm == null)
                    return;

                string scene = gm.GetCurrentScene();

                // Only do this in Great Ruins.
                if (scene != TargetScene)
                    return;

                // Only try once per session.
                if (_attemptedThisSession)
                    return;

                _attemptedThisSession = true;

                MelonLogger.Msg("[GreatRuinsTutorialSwitchLogic] A1_GreatRuins loaded. Looking for tutorial levers...");

                // Finds all active Switchable components in the room.
                Switchable[] switchables = UnityEngine.Object.FindObjectsOfType<Switchable>();
                if (switchables == null || switchables.Length == 0)
                {
                    MelonLogger.Warning("[GreatRuinsTutorialSwitchLogic] No Switchable objects found in A1_GreatRuins.");
                    return;
                }

                int activatedCount = 0;

                for (int i = 0; i < switchables.Length; i++)
                {
                    Switchable sw = switchables[i];
                    if ((object)sw == null || (object)sw.gameObject == null)
                        continue;

                    string name = sw.gameObject.name ?? string.Empty;
                    Vector3 pos = sw.transform.position;

                    // Matches the bottom tutorial lever by name and approximate position.
                    bool isBottom =
                        name == "LD_LeverTempleGeneric_Break" &&
                        NearlyEqual(pos.x, 8.5f) &&
                        NearlyEqual(pos.y, -9f);

                    // Matches the top tutorial lever by name and approximate position.
                    bool isTop =
                        name == "LD_LeverTempleGeneric_Break" &&
                        NearlyEqual(pos.x, 54.5f) &&
                        NearlyEqual(pos.y, -36f);

                    if (!isBottom && !isTop)
                        continue;

                    // Turns the lever on visually and logically.
                    sw.TurnOnImmediate();

                    // Saves its state so the game remembers it.
                    sw.SetSaved(true);
                    activatedCount++;

                    MelonLogger.Msg(
                        "[GreatRuinsTutorialSwitchLogic] Activated lever: " +
                        name + " at (" + pos.x + ", " + pos.y + ", " + pos.z + ")" +
                        " | SaveID=" + Safe(sw.GetUniqueSaveID()));
                }

                if (activatedCount > 0)
                {
                    MelonLogger.Msg("[GreatRuinsTutorialSwitchLogic] SUCCESS: activated " + activatedCount + " Great Ruins tutorial lever(s).");
                }
                else
                {
                    MelonLogger.Warning("[GreatRuinsTutorialSwitchLogic] WARNING: could not find the expected Great Ruins tutorial levers.");
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[GreatRuinsTutorialSwitchLogic] Exception while trying to activate tutorial levers: " + ex);
            }
        }

        // Lets us compare float positions with a small amount of tolerance.
        private static bool NearlyEqual(float a, float b)
        {
            return Mathf.Abs(a - b) <= 0.2f;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<null-or-empty>" : value;
        }
    }
}
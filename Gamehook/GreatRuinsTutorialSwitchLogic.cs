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
        private static bool _attemptedThisSession;
        private const string TargetScene = "A1_GreatRuins";

        private static void Postfix()
        {
            try
            {
                if (!SkipCutsceneConfig.Enabled)
                    return;

                if (!SkipCutsceneConfig.AutoActivateGreatRuinsTutorialSwitches)
                    return;

                var gm = PersistentSingleton<GameManager>.instance;
                if ((object)gm == null)
                    return;

                string scene = gm.GetCurrentScene();
                if (scene != TargetScene)
                    return;

                if (_attemptedThisSession)
                    return;

                _attemptedThisSession = true;

                MelonLogger.Msg("[GreatRuinsTutorialSwitchLogic] A1_GreatRuins loaded. Looking for tutorial levers...");

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

                    bool isBottom =
                        name == "LD_LeverTempleGeneric_Break" &&
                        NearlyEqual(pos.x, 8.5f) &&
                        NearlyEqual(pos.y, -9f);

                    bool isTop =
                        name == "LD_LeverTempleGeneric_Break" &&
                        NearlyEqual(pos.x, 54.5f) &&
                        NearlyEqual(pos.y, -36f);

                    if (!isBottom && !isTop)
                        continue;

                    sw.TurnOnImmediate();
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
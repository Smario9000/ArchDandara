// NewGameSkipIntroLogic.cs
// Changes the starting room on a fresh save so the player skips the intro room/cutscene.

using System;
using System.Reflection;
using HarmonyLib;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(GameManager), "GetState")]
    public static class NewGameSkipIntroLogic
    {
        private const string IntroScene = "A1_Void4";

        // Known-good fallback values.
        private const string SafeFallbackCampScene = "A1_ForestEdge";
        private const SpawnID SafeFallbackSpawn = SpawnID.Camp;

        private static void Postfix(GameManager __instance)
        {
            try
            {
                if (!SkipCutsceneConfig.Enabled)
                    return;

                if ((object)__instance == null)
                {
                    MelonLogger.Warning("[NewGameSkipIntroLogic] GameManager instance was null.");
                    return;
                }

                bool hasExistingSave = false;
                if ((object)PersistentSingleton<SaveManager>.instance != null)
                {
                    hasExistingSave = PersistentSingleton<SaveManager>.instance.Has("GameManager.GameState");
                }

                if (SkipCutsceneConfig.VerboseDebug)
                {
                    MelonLogger.Msg(
                        "[NewGameSkipIntroLogic] GetState postfix fired. HasExistingSave=" +
                        hasExistingSave);
                }

                if (hasExistingSave)
                {
                    if (SkipCutsceneConfig.VerboseDebug)
                        MelonLogger.Msg("[NewGameSkipIntroLogic] Existing save found. Intro skip not applied.");

                    return;
                }

                FieldInfo gameStateField = AccessTools.Field(typeof(GameManager), "_gameState");
                if ((object)gameStateField == null)
                {
                    MelonLogger.Error("[NewGameSkipIntroLogic] Could not find GameManager._gameState");
                    return;
                }

                GameManager.GameState state = (GameManager.GameState)gameStateField.GetValue(__instance);

                if (SkipCutsceneConfig.VerboseDebug)
                {
                    MelonLogger.Msg(
                        "[NewGameSkipIntroLogic] Original state: " +
                        "lastScene=" + Safe(state.lastScene) +
                        " currentScene=" + Safe(state.currentScene) +
                        " currentSpawnID=" + state.currentSpawnID +
                        " currentRoomNameID=" + Safe(state.currentRoomNameID));
                }

                if (state.currentScene != IntroScene)
                {
                    if (SkipCutsceneConfig.VerboseDebug)
                    {
                        MelonLogger.Msg(
                            "[NewGameSkipIntroLogic] Fresh game did not start in expected intro scene. " +
                            "Expected=" + IntroScene + " Actual=" + Safe(state.currentScene));
                    }
                    return;
                }

                // Validate config values and fall back if needed.
                string targetScene = GetSafeStartCampScene();
                SpawnID targetSpawn = SafeFallbackSpawn;

                state.lastScene = targetScene;
                state.currentScene = targetScene;
                state.currentSpawnID = targetSpawn;

                // Safer than forcing currentRoomNameID to whatever the config says.
                // Let the game resolve the proper room identity from the scene itself.
                state.currentRoomNameID = string.Empty;

                gameStateField.SetValue(__instance, state);

                MelonLogger.Msg(
                    "[NewGameSkipIntroLogic] SUCCESS: skipped first cutscene. " +
                    IntroScene + " -> " + targetScene +
                    " | Spawn=" + targetSpawn);

                if (SkipCutsceneConfig.VerboseDebug)
                {
                    GameManager.GameState updatedState = (GameManager.GameState)gameStateField.GetValue(__instance);

                    MelonLogger.Msg(
                        "[NewGameSkipIntroLogic] Updated state: " +
                        "lastScene=" + Safe(updatedState.lastScene) +
                        " currentScene=" + Safe(updatedState.currentScene) +
                        " currentSpawnID=" + updatedState.currentSpawnID +
                        " currentRoomNameID=" + Safe(updatedState.currentRoomNameID));
                }
            }
            catch (Exception ex)
            {
                MelonLogger.Error("[NewGameSkipIntroLogic] Exception while trying to skip intro: " + ex);
            }
        }

        private static string GetSafeStartCampScene()
        {
            string scene = SkipCutsceneConfig.StartCampScene;

            if (string.IsNullOrEmpty(scene))
            {
                MelonLogger.Warning("[NewGameSkipIntroLogic] StartCampScene was empty. Falling back to " + SafeFallbackCampScene);
                return SafeFallbackCampScene;
            }

            scene = scene.Trim();

            if (scene.Length == 0)
            {
                MelonLogger.Warning("[NewGameSkipIntroLogic] StartCampScene was blank. Falling back to " + SafeFallbackCampScene);
                return SafeFallbackCampScene;
            }

            if (!scene.StartsWith("A"))
            {
                MelonLogger.Warning("[NewGameSkipIntroLogic] StartCampScene looked invalid: " + scene + ". Falling back to " + SafeFallbackCampScene);
                return SafeFallbackCampScene;
            }

            return scene;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<null-or-empty>" : value;
        }
    }
}
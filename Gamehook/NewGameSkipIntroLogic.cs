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
        // Room the game normally starts in on a fresh file.
        private const string IntroScene = "A1_Void4";

        // Room we want to start in instead.
        private const string FirstPlayableScene = "A1_ForestEdge";

        // Enables extra console output for debugging this patch.
        private static readonly bool VerboseDebug = true;

        private static void Postfix(GameManager __instance)
        {
            try
            {
                // Safety check in case GameManager is not available yet.
                if (__instance == null)
                {
                    MelonLogger.Warning("[NewGameSkipIntroLogic] GameManager instance was null.");
                    return;
                }

                // Detects whether this is an existing save or a fresh game.
                bool hasExistingSave = false;
                if (PersistentSingleton<SaveManager>.instance != null)
                {
                    hasExistingSave = PersistentSingleton<SaveManager>.instance.Has("GameManager.GameState");
                }

                if (VerboseDebug)
                {
                    MelonLogger.Msg(
                        "[NewGameSkipIntroLogic] GetState postfix fired. HasExistingSave=" +
                        hasExistingSave);
                }

                // Only skip the intro on a brand new game.
                if (hasExistingSave)
                {
                    if (VerboseDebug)
                    {
                        MelonLogger.Msg("[NewGameSkipIntroLogic] Existing save found. Intro skip not applied.");
                    }
                    return;
                }

                // Reflection is used here because _gameState is a private field.
                FieldInfo gameStateField = AccessTools.Field(typeof(GameManager), "_gameState");
                if ((object)gameStateField == null)
                {
                    MelonLogger.Error("[NewGameSkipIntroLogic] Could not find GameManager._gameState");
                    return;
                }

                // Reads the current starting-state struct from the GameManager.
                GameManager.GameState state = (GameManager.GameState)gameStateField.GetValue(__instance);

                if (VerboseDebug)
                {
                    MelonLogger.Msg(
                        "[NewGameSkipIntroLogic] Original state: " +
                        "lastScene=" + Safe(state.lastScene) +
                        " currentScene=" + Safe(state.currentScene) +
                        " currentSpawnID=" + state.currentSpawnID +
                        " currentRoomNameID=" + Safe(state.currentRoomNameID));
                }

                // Only patch the state if it is still using the normal intro room.
                if (state.currentScene != IntroScene)
                {
                    if (VerboseDebug)
                    {
                        MelonLogger.Msg(
                            "[NewGameSkipIntroLogic] Fresh game did not start in expected intro scene. " +
                            "Expected=" + IntroScene + " Actual=" + Safe(state.currentScene));
                    }
                    return;
                }

                // Rewrites the starting room to skip the intro.
                state.lastScene = FirstPlayableScene;
                state.currentScene = FirstPlayableScene;
                state.currentSpawnID = SpawnID.Camp;
                state.currentRoomNameID = FirstPlayableScene;

                // Writes the modified state back into the private GameManager field.
                gameStateField.SetValue(__instance, state);

                MelonLogger.Msg(
                    "[NewGameSkipIntroLogic] SUCCESS: skipped first cutscene. " +
                    IntroScene + " -> " + FirstPlayableScene +
                    " | Spawn=" + state.currentSpawnID);

                if (VerboseDebug)
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

        // Returns a readable placeholder if a string is null/empty.
        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "<null-or-empty>" : value;
        }
    }
}
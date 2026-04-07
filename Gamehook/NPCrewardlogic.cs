// NPCrewardlogic.cs
// Logs NPC interactions and story event unlocks.

using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(DialogueInteractable), "Interact")]
    public static class Patch_NpcInteract
    {
        // Runs when the player interacts with a dialogue-based NPC/object.
        private static void Prefix(DialogueInteractable __instance)
        {
            if (__instance == null)
                return;

            string scene = GetCurrentScene();
            string npcName = __instance.name != null ? __instance.name : "UNKNOWN_NPC";

            DataManager.LogActivity(
                "NPCInteract",
                scene,
                npcName,
                "DialogueInteractable");

            MelonLogger.Msg("[LOG][NPC] " + scene + " -> " + npcName);
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }

    [HarmonyPatch(typeof(StoryManager), "UnlockEvent")]
    public static class Patch_StoryUnlock
    {
        // Runs after the game unlocks a story event.
        private static void Postfix(StoryEvent eventID, bool __result)
        {
            if (!__result)
                return;

            string scene = GetCurrentScene();

            DataManager.LogActivity(
                "StoryUnlock",
                scene,
                eventID.ToString(),
                "UnlockEvent");

            MelonLogger.Msg("[LOG][Story] " + scene + " -> " + eventID);
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }

    // Empty holder class for organization / future logic.
    public static class NPCrewardlogic
    {
    }
}
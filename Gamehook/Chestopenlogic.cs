// Chestopenlogic.cs
// Logs when the player interacts with a chest object.

using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(PowerupInteractable), "Interact")]
    public static class Chestopenlogic
    {
        // Runs before PowerupInteractable.Interact().
        private static void Prefix(PowerupInteractable __instance)
        {
            if (__instance == null)
                return;

            // Gets the chest object's name.
            string name = __instance.name != null ? __instance.name : "UNKNOWN_CHEST";
            string lower = name.ToLower();

            // Ignores non-chest interactables that also use this base class.
            if (!lower.Contains("chest"))
                return;

            string scene = GetCurrentScene();

            // Writes this chest interaction to the checks log.
            DataManager.LogCheck(
                "ChestInteract",
                scene,
                name,
                "",
                "PowerupInteractable");

            MelonLogger.Msg("[LOG][Chest] " + scene + " -> " + name);
        }

        // Gets the current room/scene name.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }
}
// Soulcollectionlogic.cs
// Logs money gained by the player.
// Right now this is tracking AddMoney, not just soul pickups specifically.

using HarmonyLib;
using ArchDandara.Database;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    [HarmonyPatch(typeof(PlayerController), "AddMoney")]
    public static class Patch_AddMoney_Log
    {
        // Runs after the player gains money.
        private static void Postfix(int money)
        {
            string scene = GetCurrentScene();

            DataManager.LogActivity(
                "MoneyGain",
                scene,
                "PlayerController.AddMoney",
                "Amount=" + money);

            MelonLogger.Msg("[LOG][Money] " + scene + " -> +" + money);
        }

        // Gets the current scene for logging.
        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }

    // Empty holder class for organization / future logic.
    public static class Soulcollectionlogic
    {
        
    }
}
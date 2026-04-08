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
        [System.ThreadStatic]
        private static int _beforeMoney;

        private static void Prefix(PlayerController __instance, int money)
        {
            if (__instance == null || __instance.state == null)
            {
                _beforeMoney = -1;
                return;
            }

            _beforeMoney = __instance.state.currentMoney;
        }

        private static void Postfix(PlayerController __instance, int money)
        {
            if (__instance == null || __instance.state == null || _beforeMoney < 0)
                return;

            int afterMoney = __instance.state.currentMoney;
            int delta = afterMoney - _beforeMoney;

            if (delta <= 0)
                return;

            string scene = GetCurrentScene();

            DataManager.LogActivity(
                "MoneyGain",
                scene,
                "PlayerController.AddMoney",
                "Amount=" + delta);

            MelonLogger.Msg("[LOG][Money] " + scene + " -> +" + delta);
        }

        private static string GetCurrentScene()
        {
            var gm = PersistentSingleton<GameManager>.instance;
            return gm != null ? gm.GetCurrentScene() : "UNKNOWN_SCENE";
        }
    }

    public static class Soulcollectionlogic
    {
    }
}
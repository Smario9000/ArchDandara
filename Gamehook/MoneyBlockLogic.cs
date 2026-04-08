using HarmonyLib;
using MelonLoader;

namespace ArchDandara.Gamehook
{
    public static class MoneyGrantContext
    {
        // True only when the mod itself wants to allow one money grant through.
        public static bool AllowNextMoneyGrant;
    }

    [HarmonyPatch(typeof(PlayerController), "AddMoney")]
    public static class Patch_BlockVanillaMoney
    {
        // Turn this on only when you want to block normal in-game money gains.
        public static bool BlockVanillaMoney = true;

        private static bool Prefix(ref int money)
        {
            if (MoneyGrantContext.AllowNextMoneyGrant)
            {
                MoneyGrantContext.AllowNextMoneyGrant = false;
                MelonLogger.Msg("[Patch_BlockVanillaMoney] Allowed AddMoney: " + money);
                return true;
            }

            if (!BlockVanillaMoney)
                return true;

            MelonLogger.Msg("[Patch_BlockVanillaMoney] Blocked AddMoney: " + money);
            return false;
        }
    }
}
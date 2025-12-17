using System;
using MelonLoader;
using HarmonyLib;

namespace ArchDandara
{
    [HarmonyPatch(typeof(MoneyPickup), "Effect")]
    public class MoneyPickupPatch
    {
        public static void Init()
        {
            MelonLogger.Msg("MoneyPickup Patch Init");
        }

        private const int MoneyToGive = 100; // Your mod's custom amount
        private static void Postfix(MoneyPickup __instance, PlayerController player)
        { 
            // Modify money gain
            player.AddMoney(__instance.value * MoneyToGive);
            MelonLogger.Msg(ConsoleColor.Yellow,
                    "MoneyPickup Effect: Added " + (__instance.value * MoneyToGive) + " to player's money");
        }
    }
}
// MoneyLogic.cs
// Handles temporary Archipelago money send/receive test logic.

using ArchDandara.Archipelago;
using HarmonyLib;
using MelonLoader;

namespace ArchDandara.Gamehook

{
    /* 
    // Old test patch that would fully block AddMoney and redirect it.
    [HarmonyPatch(typeof(PlayerController), "AddMoney")]
    class Patch_AddMoney
    {
        static bool Prefix(ref int money)
        {
            MelonLogger.Msg("[AP][Money] Blocked AddMoney: " + money);

            APMoney.OnMoneyCollected(money);

            return false; // stop money completely
        }
    }*/
    public static class APMoney
    {
        // Called when the player gains money and you want to send an AP check.
        public static void OnMoneyCollected(int amount)
        {
            MelonLogger.Msg($"[AP][Money] Player collected: {amount}");

            if (!APServer.IsConnected)
            {
                MelonLogger.Warning("[AP][Money] Not connected, skipping send.");
                return;
            }

            try
            {
                // Temporary test location ID.
                long locationId = 190001;

                MelonLogger.Msg($"[AP][Send] Sending location check: {locationId}");

                APServer.SendLocationCheck(locationId);

                MelonLogger.Msg($"[AP][Send] SUCCESS location check sent: {locationId}");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[AP][Send] FAILED to send check: {ex.Message}");
            }
        }

        // Called when AP sends this player a money reward.
        public static void GiveMoney(int amount)
        {
            MelonLogger.Msg($"[AP][Receive] Incoming money: {amount}");

            var player = GetPlayer();

            if (player == null)
            {
                MelonLogger.Error("[AP][Receive] Player is NULL, cannot give money!");
                return;
            }

            try
            {
                int before = player.state.currentMoney;

                player.AddMoney(amount);

                int after = player.state.currentMoney;

                MelonLogger.Msg($"[AP][Receive] Money applied. Before: {before} After: {after}");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[AP][Receive] Failed to apply money: {ex.Message}");
            }
        }

        // Gets the current player object from GameManager.
        private static PlayerController GetPlayer()
        {
            return PersistentSingleton<GameManager>.instance.GetPlayer();
        }
    }
}
/*
 * ArchDandara documentation
 * Purpose: Initializes Archipelago-side lookup tables used by the mod.
 * Why: AP bootstrap is kept separate from Melon startup so the client stays idle until the player connects.
 * Notes: This startup layer intentionally does not connect; player input or the menu config decides when AP networking begins.
 */

using ArchDandara.Game;

namespace ArchDandara.Archipelago
{
    public static class APServer
    {
        private static int ManualItemIndex;

        public static void Initialize()
        {
            ManualItemIndex = SaveSync.ProcessedReceivedItemCount;
            MLLog.Msg("[APServer] Initializing...");
        }

        public static void ReceiveItem(string itemName, string playerName)
        {
            MLLog.Msg("[APServer] Received Item: " + itemName + "form" + playerName);

            ManualItemIndex++;
            APItemReceiver.Enqueue(ManualItemIndex, itemName);
        }

        public static void ReceiveTrap(string trapName)
        {
            MLLog.Msg("[APServer] Received Trap: " + trapName);
        }

        public static void ReceiveDeathLink(string playerName)
        {
            MLLog.Msg("[APServer] DeathLink from " + playerName);
        }
    }
}

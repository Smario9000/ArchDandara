/*
 * ArchDandara documentation
 * Purpose: Blocks boss-room checks until required AP boss keys are owned and shows hint text.
 * Why: Boss locks are AP progression gates, so blocked chests and NPCs need clear feedback instead of silent failure.
 * Notes: Gate messages are built here so chest and NPC patches present the same wording and hint behavior.
 */

using ArchDandara.Archipelago;

namespace ArchDandara.Game
{
    public static class InteractionGateService
    {
        private static readonly string[] BugMessages =
        {
            "Go bug them until they pick it up.",
            "Politely bother them about your progression.",
            "Tell them your route is waiting on their check.",
            "Send them a reminder before the door wins.",
            "Ask them to grab it when they can."
        };

        private static int BugMessageIndex;
        private static string ActiveMessage;
        private static string ActiveRequiredItem;
        private static string ActiveCheckType;
        private static string ActiveAction;
        private static string ActiveBugMessage;

        public static bool ShouldBlockChest(string roomId)
        {
            return ShouldBlock(roomId, "Chest", "Open", GetRequiredItemForChestRoom(roomId));
        }

        public static bool ShouldBlockNpc(string roomId)
        {
            return ShouldBlock(roomId, "NPC", "Interactable", GetRequiredItemForNpcRoom(roomId));
        }

        public static bool ShouldBlockStoryEvent(StoryEvent storyEvent)
        {
            string requiredItem;
            if (!ItemIds.TryGetItemNameForStoryEvent(storyEvent, out requiredItem))
                return false;

            return ShouldBlock("", "Trial", "Continue", requiredItem);
        }

        private static string GetRequiredItemForChestRoom(string roomId)
        {
            if (roomId == "A1_BossFightRoom")
                return "Boss StoryEvent Key 1";

            if (roomId == "A2_Boss")
                return "Boss StoryEvent Key 2";

            return null;
        }

        private static string GetRequiredItemForNpcRoom(string roomId)
        {
            if (roomId == "A2_Boss")
                return "Boss StoryEvent Key 2";

            return null;
        }

        private static bool ShouldBlock(string roomId, string checkType, string action, string requiredItem)
        {
            if (string.IsNullOrEmpty(requiredItem))
                return false;

            if (SaveSync.GetReceivedItemCount(requiredItem) > 0)
                return false;

            bool hintRequested = APClient.RequestHint(requiredItem);
            APItemLocation itemLocation = APClient.FindItemLocation(requiredItem);
            string bugMessage = NextBugMessage();
            string message = BuildMessage(checkType, action, requiredItem, itemLocation, bugMessage);

            MLLog.Msg("[Gate] " + roomId + " blocked " + checkType + ": " + message +
                            " | hintRequested=" + hintRequested);
            RememberActiveMessage(message, requiredItem, checkType, action, bugMessage);
            ShowInGameMessage(message);
            return true;
        }

        public static void OnHintLocationUpdated(APItemLocation itemLocation)
        {
            if (object.ReferenceEquals(itemLocation, null) ||
                string.IsNullOrEmpty(ActiveRequiredItem) ||
                itemLocation.ItemName != ActiveRequiredItem)
                return;

            ActiveMessage = BuildMessage(ActiveCheckType, ActiveAction, ActiveRequiredItem, itemLocation,
                ActiveBugMessage);
            ShowInGameMessage(ActiveMessage);
        }

        private static string NextBugMessage()
        {
            string message = BugMessages[BugMessageIndex % BugMessages.Length];
            BugMessageIndex++;
            return message;
        }

        private static void ShowInGameMessage(string message)
        {
            HUDManager hudManager = GameAccess.HudManager;
            if (hudManager == null)
                return;

            try
            {
                hudManager.ShowSmallText(message);
            }
            catch (System.Exception ex)
            {
                MLLog.Warning("[Gate] Failed to show in-game gate message: " +
                                    ex.GetType().Name + ": " + ex.Message);
            }
        }

        private static void RememberActiveMessage(string message, string requiredItem, string checkType,
            string action, string bugMessage)
        {
            ActiveMessage = message;
            ActiveRequiredItem = requiredItem;
            ActiveCheckType = checkType;
            ActiveAction = action;
            ActiveBugMessage = bugMessage;
        }

        private static string BuildMessage(string checkType, string action, string requiredItem,
            APItemLocation itemLocation, string bugMessage)
        {
            return "This " + checkType + " will not " + action + ". `" + requiredItem +
                   "` is Located in " + itemLocation.PlayerName + " at " + itemLocation.LocationName +
                   ", " + bugMessage;
        }

    }
}

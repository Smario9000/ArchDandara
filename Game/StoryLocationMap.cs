/*
 * ArchDandara documentation
 * Purpose: Maps StoryEvents that are real AP checks to display names and location ids.
 * Why: The game fires many non-location StoryEvents, so this separates real checks from noise.
 * Notes: Story events not listed here are treated as non-location events by the Story patch.
 */

using ArchDandara.Archipelago;

namespace ArchDandara.Game
{
    public static class StoryLocationMap
    {
        public static bool TrySendStoryLocation(StoryEvent storyEvent, string sceneName)
        {
            string[] locationNames;
            if (!TryGetLocationNames(storyEvent, out locationNames))
                return false;

            if (locationNames == null || locationNames.Length == 0)
            {
                TrySendGoal(storyEvent);
                MLLog.Msg("[StoryLocationMap] Saw AP goal story event with no sendable AP location yet: " +
                                storyEvent);
                return true;
            }

            for (int i = 0; i < locationNames.Length; i++)
            {
                string locationName = locationNames[i];
                long locationId;
                if (!LocationIds.TryGetLocationIdByName(locationName, out locationId))
                {
                    MLLog.Warning("[StoryLocationMap] Location name is not in AP table: " + locationName);
                    continue;
                }

                APLocationSender.TrySend(locationId, "StoryEvent:" + storyEvent + "=>" + locationName);
            }

            TrySendGoal(storyEvent);
            return true;
        }

        public static bool TryGetLocationName(StoryEvent storyEvent, out string locationName)
        {
            string[] locationNames;
            if (!TryGetLocationNames(storyEvent, out locationNames) || locationNames == null ||
                locationNames.Length == 0)
            {
                locationName = null;
                return false;
            }

            locationName = locationNames[0];
            return true;
        }

        private static bool TryGetLocationNames(StoryEvent storyEvent, out string[] locationNames)
        {
            switch (storyEvent)
            {
                case StoryEvent.Boss_Kill_1:
                    locationNames = new[] { "Kill Boss 1" };
                    return true;
                case StoryEvent.Boss_Kill_2:
                    locationNames = new[] { "Kill Boss 2" };
                    return true;
                case StoryEvent.PU_Stone_Creation:
                    locationNames = new[] { "Temple of Creation (3 NPC)" };
                    return true;
                case StoryEvent.FinalBoss_Kill:
                case StoryEvent.Boss_Kill_6:
                    locationNames = new[] { "Kill Eldar" };
                    return true;
                case StoryEvent.D_DLCF_MastersIntroduction:
                    locationNames = new[]
                    {
                        "The Grand Stage (NPC 1)",
                        "The Grand Stage (NPC 2)",
                        "The Grand Stage (NPC 3)"
                    };
                    return true;
                case StoryEvent.D_DLCF_ExplorerStart:
                    locationNames = new[] { "Nakaturen Frigate Elevator (NPC 5)" };
                    return true;
                case StoryEvent.D_DLCF_NobleStart:
                    locationNames = new[] { "Castle Entrance (NPC 6)" };
                    return true;
                case StoryEvent.D_DLCF_PersistentStart:
                    locationNames = new[] { "Rock Cave (NPC 7)" };
                    return true;
                case StoryEvent.DLCF_TimeFlagTrap:
                    locationNames = new[] { "Pain Sanctuary (NPC 9)" };
                    return true;
                case StoryEvent.D_DLCF_ExplorerFinish:
                    locationNames = new[] { "Wrecked Nakaturen Bridge (NPC 8)" };
                    return true;
                case StoryEvent.D_DLCF_NobleFinish:
                    locationNames = new[] { "Rest of the Acolyte (NPC 10)" };
                    return true;
                case StoryEvent.D_DLCF_PersistentFinish:
                    locationNames = new[] { "Rock Cave (NPC 11)" };
                    return true;
                case StoryEvent.DLCF_FearEnded:
                    locationNames = new string[0];
                    return true;
                default:
                    locationNames = null;
                    return false;
            }
        }

        private static void TrySendGoal(StoryEvent storyEvent)
        {
            if (!APSlotSettings.IsGoalStoryEvent(storyEvent))
                return;

            APClient.SendGoalAchieved("StoryEvent:" + storyEvent);
        }
    }
}

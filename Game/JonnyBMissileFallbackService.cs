/*
 * ArchDandara documentation
 * Purpose: Handles the special Jonny B missile altar fallback check.
 * Why: That altar can bypass normal weapon altar flow, so it needs a targeted reliable check sender.
 * Notes: This fallback should stay room-scoped to A1_GD14 so Char_GameDev does not become a global missile check.
 */

using ArchDandara.Archipelago;
using ArchDandara.Patches;
using UnityEngine;

namespace ArchDandara.Game
{
    public static class JonnyBMissileFallbackService
    {
        private const string SceneName = "A1_GD14";
        private const string MissileLocationName = "Missile Alter";

        public static bool TrySendFromStoryEvent(StoryEvent eventId)
        {
            if (eventId != StoryEvent.Char_GameDev || GameAccess.CurrentScene != SceneName)
                return false;

            return TrySendMissileAltarCheck("Story|" + SceneName + "|Char_GameDev=>" + MissileLocationName,
                "Char_GameDev");
        }

        public static bool TrySendMissileAltarCheck(string debugName, string logSource)
        {
            if (GameAccess.CurrentScene != SceneName)
                return false;

            long locationId;
            if (!LocationIds.TryGetLocationIdByName(MissileLocationName, out locationId))
            {
                MLLog.Warning("[JonnyBMissile] Could not resolve location: " + MissileLocationName);
                return false;
            }

            bool sent = APLocationSender.TrySend(locationId, debugName);
            bool checkedAlready = SaveSync.HasCheckedLocation(locationId);
            MLLog.Msg("[JonnyBMissile] " + logSource + " sent Missile Alter check | location=" + locationId +
                            " | checked=" + checkedAlready +
                            " | sent=" + sent);
            return sent || checkedAlready;
        }

        public static void TrySendFromInteraction(IInteractable interactable, string source)
        {
            if (GameAccess.CurrentScene != SceneName || object.ReferenceEquals(interactable, null))
                return;

            MonoBehaviour behaviour = interactable as MonoBehaviour;
            if (object.ReferenceEquals(behaviour, null))
                return;

            string objectName = GetInteractableName(interactable, behaviour);
            bool relevant = interactable is WeaponAltar ||
                            interactable is PowerupInteractable ||
                            interactable is ChestInteractable ||
                            interactable is DialogueInteractable ||
                            Contains(objectName, "GameDev") ||
                            Contains(objectName, "AltarMissile") ||
                            Contains(objectName, "Missile");

            MLLog.Msg("[JonnyBMissile] " + source + " interaction in " + SceneName +
                            " | type=" + interactable.GetType().Name +
                            " | object=" + objectName +
                            " | relevant=" + relevant);

            if (!relevant)
                return;

            long locationId;
            if (!LocationIds.TryGetLocationIdByName(MissileLocationName, out locationId))
            {
                MLLog.Warning("[JonnyBMissile] Could not resolve location: " + MissileLocationName);
                return;
            }

            TrySendMissileAltarCheck("Interact|" + SceneName + "|" + objectName + "=>" + MissileLocationName,
                "Interaction");
        }

        private static string GetInteractableName(IInteractable interactable, MonoBehaviour behaviour)
        {
            PowerupInteractable powerup = interactable as PowerupInteractable;
            if (!object.ReferenceEquals(powerup, null))
                return LocationName.ForPowerupInteractable(powerup);

            ChestInteractable chest = interactable as ChestInteractable;
            if (!object.ReferenceEquals(chest, null))
                return LocationName.ForChest(chest);

            return LocationName.ForObject(behaviour, "UNKNOWN_INTERACTABLE");
        }

        private static bool Contains(string value, string part)
        {
            return !string.IsNullOrEmpty(value) && value.IndexOf(part) >= 0;
        }
    }
}

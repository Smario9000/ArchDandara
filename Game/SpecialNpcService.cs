/*
 * ArchDandara documentation
 * Purpose: Handles special NPC checks that do not fit the generic NPC interaction path.
 * Why: Some dialogue rewards trigger through story events rather than standard interactables.
 * Notes: Use this for NPCs whose reward is delayed by dialogue or story events rather than immediate interaction.
 */

using ArchDandara.Archipelago;
using UnityEngine;

namespace ArchDandara.Game
{
    public static class SpecialNpcService
    {
        private static float NextUpdate;
        private static bool LoggedLazuliMissing;

        public static void Update()
        {
            if (Time.time < NextUpdate)
                return;

            NextUpdate = Time.time + 0.5f;

            if (GameAccess.CurrentScene != "A1_CreationStoneRoom")
                return;

            long locationId;
            if (!LocationIds.TryGetLocationIdByName("Temple of Creation (3 NPC)", out locationId))
                return;

            if (SaveSync.HasCheckedLocation(locationId))
                return;

            bool found = false;
            Object[] objects = Resources.FindObjectsOfTypeAll(typeof(DialogueInteractable));
            for (int i = 0; i < objects.Length; i++)
            {
                DialogueInteractable interactable = objects[i] as DialogueInteractable;
                if (object.ReferenceEquals(interactable, null))
                    continue;

                if (!SpecialNpcChecks.IsCreationStoneLazuli(interactable))
                    continue;

                found = true;
                EnableInteractable(interactable);
            }

            if (!found && !LoggedLazuliMissing)
            {
                LoggedLazuliMissing = true;
                MLLog.Msg("[SpecialNPC] Lazuli creation-stone NPC not found by scan; interaction fallback is active.");
            }
        }

        private static void EnableInteractable(DialogueInteractable interactable)
        {
            Transform transform = interactable.transform;
            while (!object.ReferenceEquals(transform, null))
            {
                if (!object.ReferenceEquals(transform.gameObject, null) && !transform.gameObject.activeSelf)
                    transform.gameObject.SetActive(true);

                transform = transform.parent;
            }

            interactable.enabled = true;
            DialogueSystem.DialogueController controller = interactable.GetComponent<DialogueSystem.DialogueController>();
            if (!object.ReferenceEquals(controller, null))
                controller.enabled = true;

            MLLog.Msg("[SpecialNPC] Forced Lazuli creation-stone NPC active: " +
                            SpecialNpcChecks.GetPath(interactable.transform));
        }
    }

    public static class SpecialNpcChecks
    {
        public static bool TryGetLocation(DialogueInteractable interactable, out long locationId)
        {
            locationId = 0;

            if (!IsCreationStoneRoomInteraction(interactable))
                return false;

            return LocationIds.TryGetLocationIdByName("Temple of Creation (3 NPC)", out locationId);
        }

        public static bool TrySendCreationStoneInteraction(IInteractable interactable, string source)
        {
            if (!IsCreationStoneRoomInteraction(interactable))
                return false;

            long locationId;
            if (!LocationIds.TryGetLocationIdByName("Temple of Creation (3 NPC)", out locationId))
            {
                MLLog.Warning("[SpecialNPC] Could not resolve Temple of Creation (3 NPC).");
                return false;
            }

            string objectName = GetInteractableName(interactable);
            bool sent = APLocationSender.TrySend(locationId,
                LocationKey.Build("NPC", GameAccess.CurrentRoomName, objectName));
            bool checkedAlready = SaveSync.HasCheckedLocation(locationId);
            MLLog.Msg("[SpecialNPC] " + source + " sent Creation Stone NPC check | object=" + objectName +
                            " | location=" + locationId +
                            " | checked=" + checkedAlready +
                            " | sent=" + sent);
            return sent || checkedAlready;
        }

        public static bool TrySendCreationStoneStoryEvent(StoryEvent eventId)
        {
            if (eventId != StoryEvent.Char_Lazuli || GameAccess.CurrentScene != "A1_CreationStoneRoom")
                return false;

            long locationId;
            if (!LocationIds.TryGetLocationIdByName("Temple of Creation (3 NPC)", out locationId))
            {
                MLLog.Warning("[SpecialNPC] Could not resolve Temple of Creation (3 NPC).");
                return false;
            }

            bool sent = APLocationSender.TrySend(locationId,
                "StoryEvent:Char_Lazuli=>Temple of Creation (3 NPC)");
            bool checkedAlready = SaveSync.HasCheckedLocation(locationId);
            MLLog.Msg("[SpecialNPC] Char_Lazuli sent Creation Stone NPC check | location=" + locationId +
                            " | checked=" + checkedAlready +
                            " | sent=" + sent);
            return sent || checkedAlready;
        }

        public static bool IsCreationStoneLazuli(DialogueInteractable interactable)
        {
            if (object.ReferenceEquals(interactable, null))
                return false;

            if (GameAccess.CurrentScene != "A1_CreationStoneRoom")
                return false;

            string path = GetPath(interactable.transform).ToLower();
            return path.IndexOf("lazuli") >= 0 ||
                   path.IndexOf("lazúli") >= 0 ||
                   path.IndexOf("lazãºli") >= 0;
        }

        public static string GetPath(Transform transform)
        {
            if (object.ReferenceEquals(transform, null))
                return "null";

            string path = transform.name;
            Transform parent = transform.parent;
            while (!object.ReferenceEquals(parent, null))
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        private static bool IsCreationStoneRoomInteraction(IInteractable interactable)
        {
            if (object.ReferenceEquals(interactable, null))
                return false;

            if (GameAccess.CurrentScene != "A1_CreationStoneRoom")
                return false;

            DialogueInteractable dialogue = interactable as DialogueInteractable;
            if (!object.ReferenceEquals(dialogue, null))
                return true;

            MonoBehaviour behaviour = interactable as MonoBehaviour;
            if (object.ReferenceEquals(behaviour, null))
                return false;

            string path = GetPath(behaviour.transform).ToLower();
            return path.IndexOf("lazuli") >= 0 ||
                   path.IndexOf("laz") >= 0 ||
                   path.IndexOf("creation") >= 0 ||
                   path.IndexOf("stone") >= 0;
        }

        private static string GetInteractableName(IInteractable interactable)
        {
            MonoBehaviour behaviour = interactable as MonoBehaviour;
            if (object.ReferenceEquals(behaviour, null))
                return "UNKNOWN_NPC";

            return !string.IsNullOrEmpty(behaviour.name) ? behaviour.name : "UNKNOWN_NPC";
        }
    }
}

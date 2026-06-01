/*
 * ArchDandara documentation
 * Purpose: Helpers for reading stable scene and object names from game objects.
 * Why: AP location resolution needs useful names even when Unity object paths are generated or nested.
 * Notes: This helper exists so patches do not each invent their own object-path formatting.
 */

using HarmonyLib;
using UnityEngine;

namespace ArchDandara.Patches
{
    public static class LocationName
    {
        public static string ForObject(Object obj, string fallback)
        {
            if (object.ReferenceEquals(obj, null) || string.IsNullOrEmpty(obj.name))
                return fallback;

            return obj.name;
        }

        public static string ForChest(ChestInteractable chest)
        {
            string objectName = ForObject(chest, "UNKNOWN_CHEST");
            string uniqueId = TryGetChestUniqueId(chest);
            if (!string.IsNullOrEmpty(uniqueId))
                objectName = objectName + "#" + uniqueId;

            return objectName;
        }

        public static string ForPowerupInteractable(PowerupInteractable interactable)
        {
            string objectName = ForObject(interactable, "UNKNOWN_POWERUP_INTERACTABLE");
            string uniqueId = TryGetPowerupInteractableUniqueId(interactable);
            if (!string.IsNullOrEmpty(uniqueId))
                objectName = objectName + "#" + uniqueId;

            StoryEvent storyEvent = TryGetPowerupInteractableStoryEvent(interactable);
            if (storyEvent != StoryEvent.None)
                objectName = objectName + "#" + storyEvent;

            return objectName;
        }

        public static string TryGetChestUniqueId(ChestInteractable chest)
        {
            string uniqueId = TryCallStringMethod(chest, "GetUniqueID");
            if (!string.IsNullOrEmpty(uniqueId))
                return uniqueId;

            return TryReadStringField(chest, "_uniqueSceneID");
        }

        public static string TryGetPowerupInteractableUniqueId(PowerupInteractable interactable)
        {
            string uniqueId = TryCallStringMethod(interactable, "GetUniqueID");
            if (!string.IsNullOrEmpty(uniqueId))
                return uniqueId;

            return TryReadStringField(interactable, "_uniqueSceneID");
        }

        public static StoryEvent TryGetPowerupInteractableStoryEvent(PowerupInteractable interactable)
        {
            object value = TryReadField(interactable, "_storyEvent");
            if (value is StoryEvent)
                return (StoryEvent)value;

            return StoryEvent.None;
        }

        public static bool TryGetChestIsUsed(ChestInteractable chest)
        {
            object value = TryCallMethod(chest, "IsUsed");
            if (value is bool)
                return (bool)value;

            return false;
        }

        public static bool TryGetPowerupInteractableIsUsed(PowerupInteractable interactable)
        {
            object value = TryCallMethod(interactable, "IsUsed");
            if (value is bool)
                return (bool)value;

            return false;
        }

        public static void ForcePowerupInteractableUsedVisual(PowerupInteractable interactable)
        {
            if (object.ReferenceEquals(interactable, null))
                return;

            try
            {
                Animator animator = interactable.GetComponent<Animator>();
                if (!object.ReferenceEquals(animator, null))
                    animator.SetBool("On", false);
            }
            catch
            {
            }
        }

        private static string TryReadStringField(object instance, string fieldName)
        {
            return TryReadField(instance, fieldName) as string;
        }

        private static object TryReadField(object instance, string fieldName)
        {
            if (object.ReferenceEquals(instance, null))
                return null;

            try
            {
                System.Reflection.FieldInfo field = AccessTools.Field(instance.GetType(), fieldName);
                if (object.ReferenceEquals(field, null))
                    field = AccessTools.Field(instance.GetType().BaseType, fieldName);

                if (object.ReferenceEquals(field, null))
                    return null;

                return field.GetValue(instance);
            }
            catch
            {
                return null;
            }
        }

        private static string TryCallStringMethod(object instance, string methodName)
        {
            return TryCallMethod(instance, methodName) as string;
        }

        private static object TryCallMethod(object instance, string methodName)
        {
            if (object.ReferenceEquals(instance, null))
                return null;

            try
            {
                System.Reflection.MethodInfo method = AccessTools.Method(instance.GetType(), methodName);
                if (object.ReferenceEquals(method, null) && !object.ReferenceEquals(instance.GetType().BaseType, null))
                    method = AccessTools.Method(instance.GetType().BaseType, methodName);

                if (object.ReferenceEquals(method, null))
                    return null;

                return method.Invoke(instance, null);
            }
            catch
            {
                return null;
            }
        }
    }
}

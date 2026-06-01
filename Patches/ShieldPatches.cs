/*
 * ArchDandara documentation
 * Purpose: Lets shield use normal ammo even if no special weapon has been granted yet.
 * Why: Vanilla shield assumes a special shot exists; AP can give shield first.
 * Notes: The fallback only activates for owned and enabled shield so regular weapon ammo checks still behave normally.
 */

using ArchDandara.Game;
using HarmonyLib;

namespace ArchDandara.Patches
{
    [HarmonyPatch(typeof(PlayerController), "HasAmmo", new System.Type[0])]
    public static class PlayerHasAmmoForShieldPatch
    {
        private static void Postfix(PlayerController __instance, ref bool __result)
        {
            if (__result || !ShouldUseShieldAmmoFallback(__instance))
                return;

            __result = __instance.Gun != null && __instance.Gun.HasAmmo();
        }

        private static bool ShouldUseShieldAmmoFallback(PlayerController player)
        {
            if (player == null || player.Gun == null)
                return false;

            StoryManager storyManager = GameAccess.StoryManager;
            if (object.ReferenceEquals(storyManager, null))
                return false;

            StoryEvent shieldEvent = player.eventNeededForShield;
            if (shieldEvent == StoryEvent.None)
                shieldEvent = StoryEvent.PU_Shield;

            if (!storyManager.GetEvent(shieldEvent) && !storyManager.GetEvent(StoryEvent.PU_Shield))
                return false;

            return IsShieldEnabled(player);
        }

        private static bool IsShieldEnabled(PlayerController player)
        {
            try
            {
                System.Reflection.FieldInfo field = typeof(PlayerController).GetField("_shield",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (object.ReferenceEquals(field, null))
                    return false;

                ShieldController shield = field.GetValue(player) as ShieldController;
                return !object.ReferenceEquals(shield, null) && shield.enabled;
            }
            catch
            {
                return false;
            }
        }
    }
}

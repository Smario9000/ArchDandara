/*
 * ArchDandara documentation
 * Purpose: Marks when a powerup or story unlock is being applied by AP or by an approved vanilla interaction.
 * Why: Reward-blocking patches need this context so AP grants are not blocked by our own hooks.
 * Notes: Always reset these flags in finally blocks; stale grant context can let vanilla rewards leak through.
 */

namespace ArchDandara.Game
{
    public static class GrantContext
    {
        public static bool IsArchipelagoGrant;
        public static bool IsVanillaPowerupInteraction;
    }
}

// DebugLogger.cs
// Simple toggleable debug logger for extra console output while testing.

using MelonLoader;
using UnityEngine;

namespace ArchDandara.Debugging
{
    public static class DebugLogger
    {
        // Tracks whether debug logging is currently on.
        public static bool Enabled = false;

        // Flips debug logging on or off.
        public static void Toggle()
        {
            Enabled = !Enabled;
            MelonLogger.Msg("[DEBUG] Debug logging " + (Enabled ? "ENABLED" : "DISABLED"));
        }

        // Prints a debug message only if debug mode is enabled.
        public static void Log(string message)
        {
            if (!Enabled) return;
            MelonLogger.Msg("[DEBUG] " + message);
        }

        // Shift + F10 toggles debug mode at runtime.
        public static void CheckKeyToggle()
        {
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.F10))
            {
                Toggle();
            }
        }
    }
}
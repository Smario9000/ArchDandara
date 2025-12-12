//MainMod.cs

// ====================================================================================================
//  ArchDandara — Fully Commented Mod File
//  This version contains MAXIMUM explanation for beginners and future maintainers.
//  Every system is documented: MelonLoader, Harmony, logging, HSV → RGB, patching, filtering, etc.
// ====================================================================================================

// This manager handles saving and loading door metadata to/from JSON.
// It is designed for Unity (Mono) + MelonLoader mods.
// The goal: Allow you to rewrite what door leads to what room by editing JSON.
// This version is clean, commented, and expandable.

using System;
using MelonLoader;                // Main MelonLoader API (MelonMod, MelonInfo, logging system)
using HarmonyLib;
using MelonLoader.Logging;
using UnityEngine;                // Unity game engine types (Debug.Log, GameObject, Time, etc.)


// ====================================================================================================
//  MELONLOADER METADATA ATTRIBUTES
// ----------------------------------------------------------------------------------------------------
//  • MelonInfo tells MelonLoader what your mod is called, version, and who made it
//  • MelonGame restricts the mod so it only loads when the targeted game is running
// ====================================================================================================
//[assembly: MelonInfo(typeof(ArchDandara.MainMod), "ArchDandara", "0.0.1", "Smores9000")]
[assembly: MelonInfo(typeof(ArchDandara.MainMod), "ArchDandara", "0.0.3", "Smores9000")]
[assembly: MelonGame("Long Hat House", "Dandara")]

namespace ArchDandara
{
    public class MainMod : MelonMod
    {
        //Object Oriented Programming
        public static DoorJsonManager DoorJsonManager { get; private set; }
        public static RoomDoorScanner RoomDoorScanner { get; private set; }
        // ============================================================================================
        //  HARMONY — Runtime Code Patching
        // --------------------------------------------------------------------------------------------
        // Harmony allows us to modify existing game functions at runtime.
        // Example: patching UnityEngine.Debug.Log so all logs pass through our colored logger.
        // ============================================================================================
        private HarmonyLib.Harmony _harmony;

        // ============================================================================================
        //  LOGGING MODE ENUM — determines how logs are printed
        // --------------------------------------------------------------------------------------------
        // Normal → white only
        // Color  → fixed color per log level
        // RGB    → full rainbow (HSV cycling)
        // ============================================================================================
        private enum LogColorMode
        {
            Normal, // No colors, pure white
            Color, // Static color per category
            RGB // Smoothly changing hue per log
        }

        // Set current mode here (RGB = animated rainbow)
        private static readonly LogColorMode LOGMode = LogColorMode.RGB;

        // ============================================================================================
        //  FIXED COLOR SET — ColorARGB from MelonLoader (NOT System.ConsoleColor!)
        // --------------------------------------------------------------------------------------------
        //  ColorARGB uses 0–255 bytes per channel and works with MelonLogger.
        // ============================================================================================
        private static readonly ColorARGB InfoColor = ColorARGB.Cyan; // Bright cyan
        private static readonly ColorARGB WarningColor = ColorARGB.Yellow; // Warning yellow
        private static readonly ColorARGB ErrorColor = ColorARGB.Red; // Critical red
        private static readonly ColorARGB DebugColor = ColorARGB.Green; // Debug green
        private static readonly ColorARGB DefaultColor = ColorARGB.White; // Default white

        // ============================================================================================
        //  HSV HUE TRACKER — stores our current rainbow cycle value
        // --------------------------------------------------------------------------------------------
        // hue goes 0 → 1 and wraps around. This animates the rainbow color.
        // ============================================================================================
        private static float _rgbHue;



        // ============================================================================================
        //  MOD INITIALIZATION — runs ONE TIME when MelonLoader loads the mod
        // --------------------------------------------------------------------------------------------
        //  PERFECT for:
        //  • Applying Harmony patches
        //  • Loading config/settings
        //  • Initial logging
        // ============================================================================================
        public override void OnInitializeMelon()
        {
            // First message confirming the mod loaded successfully
            MelonLogger.Msg(DefaultColor, "ArchDandara Mod Loaded — Now with Full Documentation!");
            DoorJsonManager = new DoorJsonManager(); // MUST run before scanner
            //MainMod.DoorJsonManager = DoorJsonManager; <----- This is how to call it
            RoomDoorScanner = new RoomDoorScanner();       // Scanner registers events but does NOT manually call scene load
            // Create Harmony instance and apply all patches inside this assembly
            _harmony = new HarmonyLib.Harmony("com.you.archdandara");
            _harmony.PatchAll();
        }



        // ============================================================================================
        //  PrintWithColor — Core logging function for ALL colored logs
        // --------------------------------------------------------------------------------------------
        //  Behavior:
        //   • NORMAL → Always white
        //   • COLOR  → Uses provided static color
        //   • RGB    → Converts hue → RGB smoothly and increments hue
        // ============================================================================================
        private static void PrintWithColor(string msg, ColorARGB baseColor)
        {
            // If RGB mode: convert current hue → color
            if (LOGMode == LogColorMode.RGB)
            {
                baseColor = HueToStaticRainbow(_rgbHue);

                // Advance hue for next log
                _rgbHue += 0.03f; // Controls speed of rainbow
                if (_rgbHue > 1f)
                    _rgbHue -= 1f; // Wrap hue back to 0
            }

            // If NORMAL mode: override everything to white
            if (LOGMode == LogColorMode.Normal)
                baseColor = DefaultColor;

            // Final colored output to MelonLoader console
            MelonLogger.Msg(baseColor, msg);
        }

        private static ColorARGB HueToStaticRainbow(float hue)
        {
            hue = hue - (float)Math.Floor(hue);
            float zone = hue * 6f;

            if (zone < 1f) return ColorARGB.Red;
            if (zone < 2f) return ColorARGB.Magenta;
            if (zone < 3f) return ColorARGB.Blue;
            if (zone < 4f) return ColorARGB.Cyan;
            if (zone < 5f) return ColorARGB.Green;
            return ColorARGB.Yellow;
        }
        
        // ============================================================================================
        //  HARMONY PATCH — Intercepts UnityEngine.Debug.Log(object)
        // --------------------------------------------------------------------------------------------
        //  Behavior:
        //   • Any call to Debug.Log from the game is intercepted BEFORE executing
        //   • We can filter, rewrite, recolor, or entirely suppress logs
        // ============================================================================================
        [HarmonyPatch(typeof(Debug), "Log", new[] { typeof(object) })]
        private static class DebugLogPatch
        {
            static void Prefix(object message)
            {
                // Convert object → string safely
                string msg = message?.ToString() ?? "";

                // ------------------------------------------------------------------------------------
                // FILTER OUT NOISY LOG SPAM
                // (Game constantly prints controller/keyboard input mode spam — we remove it)
                // ------------------------------------------------------------------------------------
                if (msg.IndexOf("[INPUT MODE]", StringComparison.OrdinalIgnoreCase) >= 0)
                    return; // Skip entirely
                
                // ------------------------------------------------------------------------------------
                // Send message to our custom colorful logger
                // ------------------------------------------------------------------------------------
                PrintWithColor($"[DebugLogPatch] {msg}", DebugColor);
            }
        }


        // ============================================================================================
        //  OPTIONAL CLEAN WRAPPERS FOR LOG LEVELS
        // --------------------------------------------------------------------------------------------
        //  Use these anywhere in your code instead of PrintWithColor directly.
        // ============================================================================================
        private static void LogInfo(string msg) => PrintWithColor($"[INFO]  {msg}", InfoColor);
        private static void LogWarn(string msg) => PrintWithColor($"[WARN]  {msg}", WarningColor);
        private static void LogError(string msg) => PrintWithColor($"[ERROR] {msg}", ErrorColor);
        private static void LogDebug(string msg) => PrintWithColor($"[DEBUG] {msg}", DebugColor);
    }
}
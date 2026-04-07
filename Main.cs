// Main.cs
// Main mod entry point.
// Initializes Harmony patches, file logging, and Archipelago config.

using ArchDandara.Archipelago;
using ArchDandara.Database;
using ArchDandara.Debugging;
using MelonLoader;

[assembly: MelonInfo(typeof(ArchDandara.Main),
    "Dandara Randomizer",
    "0.0.1-12",
    "Smores9000")]
[assembly: MelonGame(
    "Long Hat House",
    "Dandara")]

namespace ArchDandara
{
    public class Main : MelonMod
    {
        // Runs once when the mod first loads.
        public override void OnInitializeMelon()
        {
            MelonLogger.Msg("Dandara Randomizer Loaded!");

            // Applies all Harmony patches in the mod.
            HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("ArchDandara.Patches");
            harmony.PatchAll();

            // Initializes output/log files.
            DataManager.Init();
            DataManager.LogActivity("ModInit", "BOOT", "Dandara Randomizer", "OnInitializeMelon");

            // Initializes Archipelago config entries.
            APClient.InitConfig();
        }

        // Runs every frame.
        public override void OnUpdate()
        {
            // Checks AP hotkeys like connect/reload/disconnect.
            APClient.OnUpdate();

            // Checks the debug toggle hotkey.
            DebugLogger.CheckKeyToggle();
        }
    }
}
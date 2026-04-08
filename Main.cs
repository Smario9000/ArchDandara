// Main.cs
// Main mod entry point.
// Initializes Harmony patches, file logging, and Archipelago config.

using ArchDandara.Archipelago;
using ArchDandara.Database;
using ArchDandara.Debugging;
using ArchDandara.Gamehook;
using MelonLoader;

[assembly: MelonInfo(typeof(ArchDandara.Main),
    "Dandara Randomizer",
    "0.0.1-13",
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
            SkipCutsceneConfig.Init();
            MelonLogger.Msg("[SkipCutsceneConfig] Press Ctrl+Shift+F1 to reload SkipCutscene.cfg");
            
            APClient.InitConfig();
        }

        // Runs every frame.
        public override void OnUpdate()
        {
            APClient.OnUpdate();
            DebugLogger.CheckKeyToggle();
            DebugItemMenu.OnUpdate();
            
            bool ctrl = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftControl) ||
                        UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightControl);

            bool shift = UnityEngine.Input.GetKey(UnityEngine.KeyCode.LeftShift) ||
                         UnityEngine.Input.GetKey(UnityEngine.KeyCode.RightShift);

            // Ctrl + Shift + F1 reloads SkipCutscene.cfg at runtime.
            if (ctrl && shift && UnityEngine.Input.GetKeyDown(UnityEngine.KeyCode.F1))
            {
                SkipCutsceneConfig.Reload();
            }
        }
        public override void OnGUI()
        {
            DebugItemMenu.OnGUI();
        }
    }
}
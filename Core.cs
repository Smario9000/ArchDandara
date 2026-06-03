/*
 * ArchDandara documentation
 * Purpose: MelonLoader entry point for initializing config, install checks, patches, AP systems, and update polling.
 * Why: Startup is centralized so it is clear what loads before gameplay and what is polled every frame.
 * Notes: Frame update polling stays short; long AP work should happen in background threads or queued services.
 */

using ArchDandara.Archipelago;
using ArchDandara.Config;
using ArchDandara.Game;
using MelonLoader;
using UnityEngine;

[assembly: MelonInfo(typeof(ArchDandara.Core),
    "Dandara Randomizer",
    "0.1.1-012", 
    "Smores9000")]
[assembly: MelonGame(
    "Long Hat House",
    "Dandara")]

namespace ArchDandara
{
    public class Core : MelonMod
    {
        public new static HarmonyLib.Harmony HarmonyInstance;

        public override void OnInitializeMelon()
        {
            // Config and logging are first so every later startup problem can be reported through
            // the same category-controlled system.
            MLDandaraConfig.Initialize();
            MLLog.Msg("Dandara Randomizer Loaded!");
            InstallCheck.Run();
            EnableBackgroundUpdate();

            // PatchAll discovers all HarmonyPatch classes in this assembly. Keep patch classes
            // small and self-contained so startup failures identify the broken feature quickly.
            HarmonyInstance = new HarmonyLib.Harmony("ArchDandara.Patches");
            HarmonyInstance.PatchAll();

            // AP tables and save state initialize before the player connects. The actual network
            // session stays idle until F3 or the menu Connect button starts APClient.
            APConfig.Initialize();
            ItemIds.Initialize();
            SaveSync.Initialize();
            LocationIds.Initialize();
            APServer.Initialize();

            PrintKeybinds();
            MLLog.Msg("AP Systems Initialized. AP client is idle until F3.");
        }

        public override void OnUpdate()
        {
            EnableBackgroundUpdate();
            HandleKeybinds();
            // Frame polling only advances lightweight state machines. Network login and other
            // expensive work is done on background threads or in queued services.
            APClient.UpdateConnectionStatus();
            APClient.PollReceivedItems();
            APItemReceiver.ProcessQueue();
            HudRefreshService.Update();
            SpecialNpcService.Update();
            WorldObjectGrantService.Update();
            ShopSaltBalanceService.Update();
            AmmoCostService.Update();
            MainMenuArchipelagoBrandingService.Update();
            MainMenuArchipelagoSettingsService.Update();
        }

        public override void OnApplicationQuit()
        {
            APClient.Disconnect();
            SaveSync.Save();
        }

        private static void HandleKeybinds()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                {
                    MLDandaraConfig.Reload();
                    MLLog.Msg("[Keybinds] Reloaded MelonLoader log config.");
                    return;
                }

                APConfig.Reload();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F2))
            {
                APConfig.Print();
                PrintKeybinds();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F3))
            {
                APClient.Connect();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F4))
            {
                APClient.Reconnect();
                return;
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                APClient.Disconnect();
            }
        }

        private static void PrintKeybinds()
        {
            MLLog.Msg("[Keybinds] F1 reload AP config | F2 print config | F3 connect | F4 reconnect | F5 disconnect");
        }

        private static void EnableBackgroundUpdate()
        {
            if (!Application.runInBackground)
            {
                Application.runInBackground = true;
                MLLog.Msg("[Focus] Enabled background update.");
            }
        }
    }
}

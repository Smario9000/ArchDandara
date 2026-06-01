# ArchDandara

ArchDandara is a MelonLoader mod and Archipelago world for **Dandara: Trials of Fear Edition**.

The mod connects Dandara to an Archipelago server, turns chests/NPCs/altars/bosses/shop buys into Archipelago checks, applies received AP items back into the game, and includes a bridge helper for hosted `archipelago.gg` rooms.

![ArchDandara banner](./Banner.png)

## Download

For normal players, use the latest GitHub Release, not the source zip.

The release package includes:

- `Mods/ArchDandara.dll`
- required mod dependency DLLs
- `UserData/ArchDandaraData/Tools/ArchipelagoWssBridge.exe`
- bridge dependency DLLs
- `ArchDandara.apworld`
- the APWorld source folder
- `Banner.png`
- `INSTALL.md`

## Install

See [INSTALL.md](./INSTALL.md) for the full setup steps.

Short version:

1. Install MelonLoader for Dandara.
2. Copy the release `Mods` files into your Dandara `Mods` folder.
3. Copy the release `UserData` files into your Dandara folder.
4. Install `ArchDandara.apworld` into Archipelago's custom worlds folder.
5. Generate a Dandara seed.
6. Edit `APDandaraConfig.cfg` or use the in-game ArchSetting menu.
7. Press `F3` in game to connect.

## Building From Source

This repository contains the mod source and APWorld source. It does not intentionally package Dandara's game assemblies in release downloads.

Requirements:

- Windows
- .NET Framework build tools / MSBuild
- NuGet package restore support
- Dandara: Trials of Fear Edition installed
- MelonLoader installed for Dandara
- local game/MelonLoader reference DLLs available where the project file expects them

Restore NuGet packages first if this is a fresh clone:

```powershell
nuget restore .\ArchDandara.sln
```

Build the release package:

```powershell
.\Tools\BuildReleasePackage.ps1
```

The script builds:

- `ArchDandara.csproj`
- `Tools/ArchipelagoWssBridge/ArchipelagoWssBridge.csproj`
- `dist/ArchDandara-release.zip`
- `dist/ArchDandara.apworld`

## Repository Layout

- `Archipelago/` - AP client, slot settings, bridge routing, DeathLink, hint/cache logic.
- `Config/` - config files, install checks, and MelonLoader log filtering.
- `Game/` - game-facing services for item grants, saves, shop visuals, HUD refreshes, and world object changes.
- `Patches/` - Harmony patches into Dandara game classes.
- `DandaraAPWorld/` - Archipelago world source and template YAML.
- `Tools/ArchipelagoWssBridge/` - local websocket bridge used for hosted AP rooms.
- `Banner.png` - project/release image asset.

## Runtime Notes

Hosted Archipelago rooms use secure websocket connections. Dandara's old Unity/Mono runtime cannot reliably connect to hosted `wss://` rooms directly, so ArchDandara routes hosted connections through the included local bridge helper.

The mod checks for required files on startup and logs clear missing-file messages when dependencies are not installed in the expected folders.

## License / Third-Party Files

This project depends on Dandara, MelonLoader, Archipelago.MultiClient.Net, websocket-sharp, Newtonsoft.Json, and HarmonyX. Those projects keep their own licenses.

Do not redistribute Dandara game files. Release packages should contain only this mod, its redistributable dependencies, the APWorld, the bridge helper, and project assets.

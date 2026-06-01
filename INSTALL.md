# ArchDandara Install Guide

This guide is for the release download from GitHub.

## 1. Install MelonLoader

Install MelonLoader for **Dandara: Trials of Fear Edition**.

After that, your Dandara folder should contain folders like:

```text
Dandara/
  Mods/
  UserData/
  UserLibs/
  MelonLoader/
```

## 2. Install The Mod

From the ArchDandara release zip, copy:

```text
Mods/*
```

into:

```text
Dandara/Mods/
```

This includes:

```text
ArchDandara.dll
0Harmony.dll
Archipelago.MultiClient.Net.dll
Newtonsoft.Json.dll
websocket-sharp.dll
```

## 3. Install The Hosted-Server Bridge

From the release zip, copy:

```text
UserData/ArchDandaraData/Tools/*
```

into:

```text
Dandara/UserData/ArchDandaraData/Tools/
```

This includes:

```text
ArchipelagoWssBridge.exe
websocket-sharp.dll
```

The bridge is used automatically when connecting to hosted `archipelago.gg` rooms.

## 4. Install The APWorld

Copy:

```text
ArchDandara.apworld
```

into Archipelago's custom worlds folder. Common locations are:

```text
C:/ProgramData/Archipelago/custom_worlds/
```

or the `custom_worlds` folder next to your Archipelago launcher.

Create the folder if it does not exist, then restart Archipelago.

## 5. Generate A YAML

After the APWorld is installed, generate a Dandara player template from Archipelago.

Edit the YAML options you want, then generate and host the seed as normal.

## 6. Configure The Mod

ArchDandara creates this config file:

```text
Dandara/UserData/ArchDandaraData/APDandaraConfig.cfg
```

Set:

```ini
Server=archipelago.gg:PORT
Slot=YourSlotName
Password=
```

You can also edit this from the main menu with the `ArchSetting` button.

## 7. Connect In Game

Useful hotkeys:

```text
F1  reload AP config
F2  print config
F3  connect
F4  reconnect
F5  disconnect
Shift+F1 reload MelonLoader log config
```

For local servers, use:

```ini
Server=localhost:PORT
```

For hosted rooms, use:

```ini
Server=archipelago.gg:PORT
```

## Troubleshooting

If the mod reports missing files, copy the missing files from the release package into the exact folder shown in the MelonLoader log.

If hosted AP connection fails, confirm these files exist:

```text
Dandara/UserData/ArchDandaraData/Tools/ArchipelagoWssBridge.exe
Dandara/UserData/ArchDandaraData/Tools/websocket-sharp.dll
```

If the APWorld does not appear in Archipelago, restart Archipelago after copying `ArchDandara.apworld`.

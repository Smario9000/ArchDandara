# Dandara Randomizer Tracker

PopTracker pack for tracking Dandara Archipelago games.

This tracker supports manual tracking and Archipelago autotracking for the Dandara APWorld used by ArchDandara.

## What This Tracker Shows

- Main progression items and upgrades.
- NPC checks and D_DLCF story events.
- Overworld and Fear World maps.
- Shop checks and AP location checks.
- AP settings from slot data:
  - Goal type
  - Ammo/Mana Cost
  - Shop Cost
- AP received item counts for consumable or duplicate-copy items.

## Requirements

- PopTracker `0.23.1` or newer.
- The Dandara APWorld and ArchDandara mod from:
  <https://github.com/Smario9000/ArchDandara>
- An Archipelago room generated with game name `Dandara`.

## Install From Zip

1. Download `DandaraAPTracker.zip` from the GitHub release.
2. Put the zip in your PopTracker packs folder:

   ```text
   <PopTracker folder>/packs/DandaraAPTracker.zip
   ```

3. Start or restart PopTracker.
4. Choose `Dandara Randomizer` from the pack list.

You can leave the pack as a zip. PopTracker can load packs directly from zip files.

## Manual Folder Install

If you want to install from the source files instead of the zip:

1. Create this folder:

   ```text
   <PopTracker folder>/packs/DandaraAPTracker
   ```

2. Copy these files and folders into it:

   ```text
   manifest.json
   versions.json
   images/
   items/
   layouts/
   locations/
   maps/
   scripts/
   ```

3. Restart PopTracker.

Do not put the tracker files inside an extra nested folder. `manifest.json` must be directly inside the tracker folder or directly at the zip root.

## Archipelago Autotracking

This pack uses PopTracker's Archipelago support.

1. Open the tracker in PopTracker.
2. Open the Archipelago connection window.
3. Connect with your AP server address, slot/player name, and password if needed.
4. The tracker will update received items and checked locations from the AP server.

The tracker reads Dandara slot data from the AP server and uses it to show the selected goal and settings. It only accepts slot data for the Dandara game.

## Settings Display

The `Settings` group shows one icon for each setting:

- Goal:
  - Eldar icon means `final_boss`.
  - Fear Boss icon means `true_ending`.
- Ammo/Mana Cost:
  - Gray arrow means no change.
  - `1/2` means half cost.
  - `1/4` means quarter cost.
  - `2x` means double cost.
- Shop Cost:
  - Gray camp means normal.
  - Percent with down arrow means cheaper.
  - Percent with up arrow means more expensive.

## File Overview

```text
manifest.json              PopTracker pack metadata and update URL.
versions.json              PopTracker update metadata.
items/items.json           Tracker item definitions.
locations/locations.json   Tracker location and map check definitions.
layouts/tracker.json       Tracker UI layout.
maps/maps.json             Map definitions.
scripts/init.lua           Pack startup script.
scripts/ap_mapping.lua     AP item/location name and ID mappings.
scripts/autotracking.lua   AP item, location, and slot-data sync.
scripts/logic.lua          PopTracker access-rule helpers.
images/                    Item, map, and overlay images.
```

## Updates

The pack uses PopTracker's `versions_url` support:

```text
https://raw.githubusercontent.com/Smario9000/ArchDandara/Tracker/versions.json
```

When a new tracker release is published, update `versions.json` with the new package version, release zip URL, SHA-256 hash, and changelog.

## Release Checklist

1. Update `manifest.json` `package_version`.
2. Rebuild `DandaraAPTracker.zip`.
3. Calculate the zip SHA-256.
4. Update `versions.json`.
5. Commit changes to the `Tracker` branch.
6. Upload `DandaraAPTracker.zip` to the GitHub release.

## Troubleshooting

- If the pack does not appear, check that `manifest.json` is at the zip root or directly inside the tracker folder.
- If AP items do not line up, check that `scripts/ap_mapping.lua` matches the APWorld `Items.py` item order.
- If locations do not clear, check that AP location names in `scripts/ap_mapping.lua` match `Locations.py`.
- If settings do not update, confirm the AP slot is a `Dandara` slot and that the APWorld exports slot data.

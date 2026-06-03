# DandaraAPTracker 0.0.25 Update Log

This update brings the PopTracker pack back in line with the current ArchDandara APWorld.

## APWorld alignment

- Updated Archipelago item ID mapping to match the current APWorld `raw_items` order.
- Added support for the new APWorld items:
  - `Dandara Arrow Damage Upgrade`
  - `Dandara Weapon Damage Upgrade`
  - `Salt's Awareness Upgrade`
- Added `A Good Bargain (70 Chest)` to tracker locations and AP autotracking mappings.
- Added counted autotracking for `Scarf of Freedom` and the new upgrade items.
- Confirmed all current APWorld item and location names are covered by tracker mappings.

## Tracker layout and display

- Added a visible `Settings` group for goal, ammo/mana cost, and shop cost.
- Changed settings display to use one staged icon per setting instead of showing every possible option.
- Added setting overlays for ammo/mana cost and shop cost values.
- Added `Overworld NPC Check 3` and mirrored it from `Temple of Creation (3 NPC)`.
- Added visible D_DLCF story event tracking.
- Arranged tracker info groups left-to-right with map tabs underneath.

## Autotracking

- Added AP slot-data handling for Dandara settings.
- Settings now update from AP server slot data for the connected Dandara player.
- Added Dandara-only slot-data guarding.
- Added location mirror syncing for tracker icons tied to checked locations.

## Fixes

- Fixed invalid `locations.json` caused by a trailing comma.
- Fixed bad image path `MusicianPowerOn.png.png`.
- Added missing `images/items/1.png` so event/check image references resolve.
- Rebuilt `DandaraAPTracker.zip`.
- Added PopTracker update metadata through `versions_url` and `versions.json`.

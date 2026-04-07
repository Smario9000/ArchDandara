# Dandara Trials of Fear Edition - Archipelago Mod

![Dandara Archipelago Mod Banner](./Banner.png)

A work-in-progress Archipelago randomizer mod for **Dandara: Trials of Fear Edition**.

This project is focused on turning Dandara into a playable Archipelago experience by building the room graph, check logic, item handling, and game hooks needed for a full randomizer integration.

## Game Info

```text
Game Name: Dandara
Game Developer: Long Hat House
Unity Version: 2018.4.12f1
Game Version: 1.4.11
Runtime Type: net35
Game Type: MonoBleedingEdge
Game Arch: x86
```

## What This Mod Has Right Now

- Archipelago connection code started
- Room scanning and room-data export
- Door logging for building room and region flow
- Chest scanning and chest interaction logging
- NPC scanning and NPC interaction logging
- Soul scanning and money gain logging
- Story event scanning and story unlock logging
- Shop purchase logging and shop upgrade scanning
- Map exploration logging for discovered rooms
- Powerup unlock logging
- Powerup proxy logging for reward sources
- New game intro skip hook
- Great Ruins tutorial lever auto-activation hook
- Text log output for debugging and reverse-engineering

## What It Does Not Have Yet

- Full Archipelago world implementation
- Final `world.py`, `regions.py`, `locations.py`, `items.py`, and `__init__.py`
- Finalized item pool
- Finalized location/check list
- Finalized progression rules
- Full shop logic converted into AP checks
- Full NPC reward logic cleanup
- Full story event cleanup
- Full duplicate/noise cleanup in every logging path
- Full testing across the entire game
- Multiplayer-ready balancing and AP rules validation

## Current Goal

The current goal is to finish gathering accurate game data so the Dandara world can be converted into a proper Archipelago implementation.

That means this mod is currently doing a lot of:

- scanning scenes
- exporting room data
- logging checks and rewards
- testing hooks
- skipping unwanted intro/tutorial flow where needed
- identifying progression gates and item sources

## Main Data Being Collected

The mod currently writes data into log and room files so the game can be mapped and analyzed.

Examples include:

- `doors.txt`
- `chests.txt`
- `npcs.txt`
- `souls.txt`
- `storyevents.txt`
- `shopupgrades.txt`
- `activity_log.txt`
- `checks_log.txt`

These files are being used to:

- build the room graph
- identify checks
- identify rewards
- identify story gates
- identify progression requirements
- help generate Archipelago world files later

## Planned Next Steps

- Clean up remaining duplicate log spam
- Improve shop logic and convert shop levels into real AP checks
- Improve NPC reward detection
- Improve soul reward detection
- Improve story event filtering
- Finalize intro skip configuration
- Finalize Great Ruins tutorial skip behavior
- Expand room and region graph coverage
- Build a more complete map of the full game
- Sort checks into progression, filler, and utility groups
- Start writing the Archipelago world files
- Test item sending and receiving with real AP progression logic
- Replace temporary test values with real location IDs and item handling
- Add better user config options
- Add more fail-safe logging where needed
- Do full progression testing from fresh save to endgame

## Notes

This is still an active work-in-progress project.

Some systems are already functional, but a lot of the current code is still focused on data collection, hook validation, and reverse-engineering the game's logic so the final Archipelago implementation can be built correctly.

## Contact

To get in touch or follow the project discussion, use this Discord thread:

<https://discord.com/channels/731205301247803413/1444298674279546971>

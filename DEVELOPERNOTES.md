# 📘 Developer Notes

Deep technical notes for contributors, maintainers, and future-you.

This document explains the internal architecture of the Dandara Archipelago Mod, its design goals, and how the systems work together.
It is intended for developers who want to understand, contribute to, or extend the project.

# 🧩 What This Mod Contains
 - The codebase includes full inline documentation across every file, written in a hybrid style:

## 🟦 “Training Manual” Style
 - Explains how modding concepts work
 - Helps new developers understand Unity, MelonLoader, and Harmony
## 🟧 “Professional Developer” Style
 - Clean, technical summaries
 - Focus on architecture and maintainability
 - This mixed-documentation approach ensures the mod is both educational and production-ready.

# 🏗 Module Overview
## ✔ MainMod.cs
 - Entry point for the mod
 - Bootstraps all subsystems
 - Initializes configs, JSON manager, room scanner
 - Applies Harmony patches
 - Contains custom color-logging + rainbow HSV log mode

# ✔ Config System
- Two config files are generated and maintained:

# ArchDandara.cfg
## Controls:
 - Enabling RoomDoorScanner
 - Logging filters (Debug, DoorJsonManager, AP, etc.)
 - Controls verbosity of the mod

# ArchDandaraAP.cfg 
## APControls:
 - Archipelago server address
 - Port
 - Player name
 - Password


# Both use a custom class:
 - ConfigFile.cs, a lightweight key=value file handler.
 - ✔ DoorJsonManager

# Handles:
 - JSON generation
 - JSON saving
 - JSON loading (Newtonsoft)
 - Creating directory paths
 - Updating the door database
 - Ensuring database remains valid even on corrupt files

# Outputs:
    UserData/Dandara_Doors/door_database.json

# This JSON becomes the backbone for:
 - Randomized door shuffling
 - Map visualization
 - Debug output

## generation compatibility
 - ✔ RoomDoorScanner

# A reverse-engineering helper that:
 - Hooks into scene loading
 - Searches all GameObjects for components named "Door"

# Extracts:
 - Scene name
 - Door name
 - Transform position
 - OtherSideScene (via reflection)
 - Adds / updates entries in the JSON database
 - Prints formatted door logs to MelonLoader
 - This scanner allows us to “discover the game’s map” simply by walking through rooms.
 - ✔ Harmony Patches (DebugLogPatch)
 - Intercepts UnityEngine.Debug.Log

# Adds support for:
 - Noise filtering
 - Colorized logging
 - Rainbow color cycling (HSV)
 - Prevents spam like [INPUT MODE] from filling logs

# 🎯 Project Goals
## 🎓 1. Educational

- The entire codebase is written to be understandable by new modders.

## 🛠 2. Maintainable
 - Clear module boundaries:
 - Config → settings
 - MainMod → initialization
 - RoomDoorScanner → game scanning
 - DoorJsonManager → JSON I/O

## 🔌 3. Archipelago-Ideas
### The foundation is built to support:
 - Item sending/receiving
 - DeathLink
 - Slot data
 - Door-rando logic

## 🔍 4. Fully Inspectable

 - Everything the mod does is printed through configurable logging categories.

# 👨‍💻 Developer Notes
## What you can expect in the codebase:
 - Detailed comments on every function, field, and subsystem
 - Explanations of Harmony patches and why they exist
 - Logging behavior guides
 - Error-handling notes
 - Step-by-step scanning breakdown
 - Safe reflection usage for obfuscated fields
 - Configuration-first design philosophy
 - Everything is documented so you can jump in, understand, and extend the project quickly.

# 🧠 Credits
## Contributor	Role
- Smores9000	
    - Developer 
    - Reverse engineering 
    - Mod architect
- Archipelago Team	
  - AP protocol & ecosystem
  - MelonLoader Team	
  - Mod loader infrastructure 
  - Dandara Community	
- Testing
  - room exploration

# Thank you to everyone helping push Dandara modding forward!

## 🔧 Contributing

Pull requests are welcome!
Feel free to help with:

## Reverse engineering Dandara internals

 - JSON door mapping / visualizer
 - Archipelago client subsystem
 - Door-randomizer logic
 - Logging system improvements

# If you don’t understand a part of the code 
  - the design is intentional:
## 📘 every system is documented so you can learn while you modify it.

# 📞 Support / Contact

 - Have questions or want to collaborate?
 - Open an issue on GitHub, 
 - or reach out in the Archipelago 
 - or Dandara modding communities.
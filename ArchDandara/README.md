# Dandara Archipelago Mod
A MelonLoader mod for **Dandara: Trials of Fear Edition** that connects the game to the
**Archipelago multiworld randomizer system**, enabling item sending/receiving, door scanning,
and automatic JSON map generation.

This project is a work-in-progress reverse-engineering and mod development effort.

---

## ✨ Features (Current & Planned)
### 🛠️ Working on
- Making the Json File still
### ✔ Implemented
- **RoomDoorScanner**
  - Automatically scans rooms for `Door` components.
  - Exports door metadata (position, target scene, spawn IDs, etc.) to JSON.
  - Helps map out the entire game from just entering rooms.
- **DoorJsonManager**
  - Creates and maintains JSON files for all detected doors.
  - Automatically updates entries when new data is found.
  
### 🔄 In Progress

- **Archipelago Client**
  - Connection handling
  - Item receive/send
  - DeathLink support (planned)

### 📌 Planned
- Full Archipelago item routing
- Randomized door shuffle based on exported JSON
- Spoiler-log generation
- Map visualizer based on scanned door positions

---

## 🛠 Requirements

| Component | Version | Notes |
|----------|---------|-------|
| **MelonLoader** | 0.7.x | Required to load the mod |
| **Unity Game: Dandara: Trials of Fear Edition** | Steam / Windows | Game executable required |
| **.NET Framework** | 4.x | JetBrains Rider or Visual Studio |
| **Newtonsoft.Json** | 12.x | Included through NuGet |

---
## ⚙ Installation (End-Users)

1. Install **MelonLoader** for Dandara.
2. Download the mod DLL from Releases (or build it yourself).
3. Place `ArchDandara.dll` in: :...\ArchDandara\Dandara\Mods
4. Launch the game.
5. The mod will automatically begin scanning rooms and generating:
A file that scan all sceens and make a json that shows u all the door info as you play `Works`
it does show you info in the MelonLoader Logs about the doors.

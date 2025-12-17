<img width="512" height="512" alt="Banner" src="https://github.com/user-attachments/assets/af0eae36-4102-4f6d-a376-159b78497892" />

# Dandara Archipelago Mod

## ArchDandara — Dandara × Archipelago Mod
A MelonLoader mod for **Dandara: Trials of Fear Edition** that adds full support for the  
**Archipelago Multiworld Randomizer**, automatic door-scanning, and JSON world-mapping.

This is an ongoing reverse-engineering + modding project focused on building a fully
functioning AP client for Dandara.

---

## ✨ Current Features

### ✔ **Working Systems**
#### 🏠 **RoomDoorScanner**
Automatically detects:
- All `Door` components inside the currently loaded scene
- Their position (X/Y/Z)
- Their destination scene (via private-field reflection)
- Their GameObject name

Every scan produces an entry in the JSON file — no manual input required.

#### 📄 **DoorJsonManager**
Handles:
- Building the door database on startup
- Writing JSON in grouped format
- Updating a door if it already exists
- Creating the directory automatically
- Pretty-printing JSON for debugging

The JSON file grows as you explore the world, eventually becoming a complete map
of every door connection in Dandara.

#### ⚙ **Custom Config System**
Your mod uses **two separate .cfg files** stored in:
- Where the game is at `...\Dandara\UserData\ArchDandara`
- **ArchDandara.cfg**
  Contains all your mod-related settings and debug flags
- **ArchDandaraAP.cfg**  
  Stores Archipelago server settings (address, port, password, player name)

Config values are generated automatically on first startup.

---

## 🔄 In Progress

### 🌐 **Archipelago Client Integration**
Partially implemented:
- Config storage for AP connection
- Logging for AP debugging
- Planned support for:
  - Server handshake
  - Item sending
  - Item receiving
  - Location checks
  - DeathLink
  - Server reconnect logic

### 🎮 **Door Randomizer System (Future Feature)**
Once the JSON is fully mapped:
- Doors will be randomized based on JSON connections
- A spoiler log will be generated
- Optional “progression-safe” mode is planned

---

## 📌 Planned Features
- Full AP item routing
- Visual map builder using scanned door positions
- In-game UI for AP
- Custom spawn rewrite logic
- Door teleporter debug mode

---

## 🛠 Requirements

| Component | Version                | Notes |
|----------|------------------------|-------|
| **MelonLoader** | 0.7.x                  | Required to load custom mods |
| **Dandara: Trials of Fear Edition** | Epic / Steam / Windows | Must be the PC version |
| **.NET Framework** | 4.x                    | For development |
| **Newtonsoft.Json** | 12.x                   | Used for JSON serialization |

---

## 📥 Installation (For End Users)

1. Install **MelonLoader 0.7.x** into Dandara.
2. Download `ArchDandara.dll`.
3. Place it inside:
   - Where the game is at `...\Dandara\Mods`
4. Start the game.
5. The mod automatically:
- Creates the config files
- Initializes the JSON database
- Begins scanning rooms as you play

### What happens during gameplay?
- Every new room is scanned only once
- Each door is added or updated inside `door_database.json`
- The MelonLoader console prints out door info, unless disabled in config

---

## 📁 File Output

### 🔍 **Door JSON Location**
  - Where the game is at `...UserData/Dandara_Doors/door_database.json`
---

## 🧩 Example JSON Output

```json
{
  "Scenes": [
    {
      "SceneName": "Tutorial_01",
      "Doors": [
        {
          "DoorName": "LeftExit",
          "OtherSideScene": "Hub_01",
          "FakeSpawnID": "",
          "PosX": -12.8,
          "PosY": 3.1,
          "PosZ": 0
        }
      ]
    }
  ]
}
```
### 📝 **Config Files**
    Where the game is at
    UserData/ArchDandara/ArchDandara.cfg
    UserData/ArchDandara/ArchDandaraAP.cfg

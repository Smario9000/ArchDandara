/*
 * ArchDandara documentation
 * Purpose: Defines AP location ids and runtime aliases used by game-object resolution.
 * Why: The game reports inconsistent object names across scenes, so aliases let checks resolve without hardcoding every patch path.
 * Notes: Add new checks here when the mod needs to resolve a game interaction to a generated AP location id.
 */

using System.Collections.Generic;

namespace ArchDandara.Archipelago
{
    public static class LocationIds
    {
        private static readonly Dictionary<string, long> LocationIdByKey = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> LocationIdByName = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> LocationIdByRuntimeObject = new Dictionary<string, long>();
        private static readonly List<LocationEntry> Locations = new List<LocationEntry>();

        public static void Initialize()
        {
            LocationIdByKey.Clear();
            LocationIdByName.Clear();
            LocationIdByRuntimeObject.Clear();
            Locations.Clear();

            Add(1, "Temple of Creation (17 Chest)");
            Add(2, "Temple of Creation (18 Chest)");
            Add(3, "Temple of Creation (3 NPC)");
            Add(4, "The Village Center (1 Chest)");
            Add(5, "Missile Alter");
            Add(6, "Buritis (13 Chest)");
            Add(7, "Dona Clara's Restaurant (14 Chest)");
            Add(8, "Beautiful Horizon Avenue (11 Chest)");
            Add(9, "Thommaz's House (2 NPC)");
            Add(10, "Garantido's Bar (12 Chest)");
            Add(11, "Lazúli's House (15 Chest)");
            Add(12, "Lazúli's House (16 Chest)");
            Add(13, "Tarsila's House (1 NPC)");
            Add(14, "Tarsila's House (10 Chest)");
            Add(15, "Paint Well (8 Chest)");
            Add(16, "Community Street (9 Chest)");
            Add(17, "Foyer of Formidable Burdens (29 Chest)");
            Add(18, "Foyer of Formidable Burdens (30 Chest)");
            Add(19, "Archive of the Child (32 Chest)");
            Add(20, "Overcast Gate Ruins (33 Chest)");
            Add(21, "Overcast Gate Ruins (34 Chest)");
            Add(22, "Overcast Gate Ruins (35 Chest)");
            Add(23, "Overcast Gate Ruins (NPC 4)");
            Add(24, "Broken Heart Archive (31 Chest)");
            Add(25, "Main Warehouse (23 Chest)");
            Add(26, "Main Warehouse (24 Chest)");
            Add(27, "Main Warehouse (25 Chest)");
            Add(28, "The Treasure (28 Chest)");
            Add(29, "Remembrance Cliff (2 Altar)");
            Add(30, "Quarter of a Distant Love (3 Altar)");
            Add(31, "Auditorium (26 Chest)");
            Add(32, "Overburden Deposits (27 Chest)");
            Add(33, "Museum Main Hall (22 Chest)");
            Add(34, "Parlance Road (45 Chest)");
            Add(35, "Eagle Highway (61 Chest)");
            Add(36, "Side Turn (37 Chest)");
            Add(37, "Side Turn (38 Chest)");
            Add(38, "The Perfect Corner (6 Altar)");
            Add(39, "Reasoning Lock (44 Chest)");
            Add(40, "Crib of Intention (4 Altar)");
            Add(41, "Back Alley (39 Chest)");
            Add(42, "Back Alley (40 Chest)");
            Add(43, "Recycle Room (41 Chest)");
            Add(44, "Corner's Club (52 Chest)");
            Add(45, "Shock Alter");
            Add(46, "Dream Lands (53 Chest)");
            Add(47, "Dream Lands (55 Chest)");
            Add(48, "Dream Lands (56 Chest)");
            Add(49, "Dream Lands (57 Chest)");
            Add(50, "Dream Lands (58 Chest)");
            Add(51, "Dream Lands (59 Chest)");
            Add(52, "Dream Lands (60 Chest)");
            Add(53, "Dream Lands (5 Altar)");
            Add(54, "Limits of Sanity (19 Chest)");
            Add(55, "Cave of Desire (51 Chest)");
            Add(56, "Confusion Ruins (6 Chest)");
            Add(57, "Confusion Ruins (7 Chest)");
            Add(58, "Structural Cave (36 Chest)");
            Add(59, "Palmares Track (2 Chest)");
            Add(60, "Pleasure Woods (3 Chest)");
            Add(61, "Pleasure Woods (4 Chest)");
            Add(62, "Pleasure Woods (5 Chest)");
            Add(63, "Wealth Lair (21 Chest)");
            Add(64, "The Golden Corner (66 Chest)");
            Add(65, "The Golden Angle (42 Chest)");
            Add(66, "The Golden Angle (43 Chest)");
            Add(67, "The Golden Storage (46 Chest)");
            Add(68, "The Golden Storage (47 Chest)");
            Add(69, "The Golden Storage (48 Chest)");
            Add(70, "The Golden Storage (49 Chest)");
            Add(71, "A Good Bargain (70 Chest)");
            Add(72, "Kill Eldar");
            Add(73, "No Pain, No Gain (62 Chest)");
            Add(74, "Command Center (63 Chest)");
            Add(75, "Greediness Space (64 Chest)");
            Add(76, "Greediness Space (65 Chest)");
            Add(77, "Miserliness Space (67 Chest)");
            Add(78, "Miserliness Space (68 Chest)");
            Add(79, "Miserliness Space (69 Chest)");
            Add(80, "Eldarian Gates (20 Chest)");
            Add(81, "Crumbling Hollow (1 Chest)");
            Add(82, "Incident Square (10 Chest)");
            Add(83, "Station Rendezvous (11 Chest)");
            Add(84, "Station Rendezvous (12 Chest)");
            Add(85, "Pusher's Assignment (13 Chest)");
            Add(86, "Waiter's Room (14 Chest)");
            Add(87, "Through The Eyes of Fear (15 Chest)");
            Add(88, "Hidden Eye (16 Chest)");
            Add(89, "Alchemist's Gallery (17 Chest)");
            Add(90, "Underlying Truth (18 Chest)");
            Add(91, "True Explorers Treasure (19 Chest)");
            Add(92, "A Dark Angle (2 Chest)");
            Add(93, "Friendly Gathering (20 Chest)");
            Add(94, "Friendly Gathering (21 Chest)");
            Add(95, "Ostentation Room (22 Chest)");
            Add(96, "Weak-Minded Segregation (23 Chest)");
            Add(97, "Secrets Beneath Masks (24 Chest)");
            Add(98, "Finite Run (3 Chest)");
            Add(99, "Students Way (4 Chest)");
            Add(100, "Pain Sanctuary (5 Chest)");
            Add(101, "Wrecked Nakaturen Bridge (6 Chest)");
            Add(102, "Nevermore a Beautiful Horizon (7 Chest)");
            Add(103, "Auditorium Hall (8 Chest)");
            Add(104, "Windy Hall (9 Chest)");
            Add(105, "Resilience Growth (3 Altar)");
            Add(106, "Future Path Gate (2 Altar)");
            Add(107, "The Grand Stage (1 Altar)");
            Add(108, "The Grand Stage (NPC 1)");
            Add(109, "The Grand Stage (NPC 2)");
            Add(110, "The Grand Stage (NPC 3)");
            Add(111, "The Grand Stage (NPC 4)");
            Add(112, "Nakaturen Frigate Elevator (NPC 5)");
            Add(113, "Castle Entrance (NPC 6)");
            Add(114, "Rock Cave (NPC 7)");
            Add(115, "Wrecked Nakaturen Bridge (NPC 8)");
            Add(116, "Pain Sanctuary (NPC 9)");
            Add(117, "Rest of the Acolyte (NPC 10)");
            Add(118, "Rock Cave (NPC 11)");
            Add(119, "Nara's Cage (NPC 12)");
            AddShopLocations(120, "Buy Upgrade ", 44);
            Add(164, "Kill Boss 1");
            Add(165, "Kill Boss 2");

            AddRuntimeAlias(56, "AB_Backtrack4HealthFlask");
            AddRuntimeAlias(56, "ChestDandaraArrow#AB_Backtrack4HealthFlask#PU_DandaraArrow");
            AddKeyAlias(56, "PowerupInteractable", "AB_Backtrack4", "ChestDandaraArrow#AB_Backtrack4HealthFlask#PU_DandaraArrow");

            AddRuntimeAlias(15, "A1_PainterPath1MapPowerup");
            AddRuntimeAlias(15, "ChestMap#A1_PainterPath1MapPowerup#PU_Map");
            AddKeyAlias(15, "PowerupInteractable", "A1_PainterPath1", "ChestMap#A1_PainterPath1MapPowerup#PU_Map");

            AddRuntimeAlias(3, "Lazuli");
            AddRuntimeAlias(3, "Lazúli");
            AddRuntimeAlias(3, "A1_CreationStoneRoom");
            AddRuntimeAlias(3, "PU_Stone_Creation");
            AddKeyAlias(3, "NPC", "A1_CreationStoneRoom", "Lazuli");
            AddKeyAlias(3, "NPC", "Temple of Creation", "Lazuli");

            AddRuntimeAlias(5, "AltarMissile");
            AddRuntimeAlias(5, "Weapon_Missile");
            AddRuntimeAlias(5, "Jonny B. Missiles");
            AddKeyAlias(5, "WeaponAltar", "Jonny B. Cave", "AltarMissile#A1_GD14#Weapon_Missile");
            AddKeyAlias(5, "WeaponAltar", "A1_GD14", "AltarMissile#A1_GD14#Weapon_Missile");

            AddRuntimeAlias(45, "AltarShock");
            AddRuntimeAlias(45, "Weapon_EnergyBall");
            AddRuntimeAlias(45, "Anxiety Shock");
            AddKeyAlias(45, "WeaponAltar", "Dream Lands", "AltarShock#A4_A4B#Weapon_EnergyBall");
            AddKeyAlias(45, "WeaponAltar", "A4_A4B", "AltarShock#A4_A4B#Weapon_EnergyBall");

            AddRuntimeAlias(71, "A3_BackSecret1");
            AddRuntimeAlias(71, "A Good Bargain");
            AddRuntimeAlias(71, "A Good Bargain-ChestMoneyExtraLarge");
            AddRuntimeAlias(71, "ChestMoneyExtraLarge#A3_BackSecret1");
            AddKeyAlias(71, "PowerupInteractable", "A3_BackSecret1",
                "A Good Bargain-ChestMoneyExtraLarge#A3_BackSecret1#PU_Money");
            AddKeyAlias(71, "PowerupInteractable", "A Good Bargain",
                "A Good Bargain-ChestMoneyExtraLarge#A3_BackSecret1#PU_Money");
            AddKeyAlias(71, "PowerupInteractable", "Intention Capital",
                "A Good Bargain-ChestMoneyExtraLarge#A3_BackSecret1#PU_Money");

            AddKeyAlias(44, "PowerupInteractable", "Corner's Club",
                "ChestMoneyMedium#A4_A3ChestMoneyMediumMapPowerup#PU_Money");
            AddKeyAlias(44, "PowerupInteractable", "Dream Lands",
                "ChestMoneyMedium#A4_A3ChestMoneyMediumMapPowerup#PU_Money");
            AddKeyAlias(44, "PowerupInteractable", "A4_A3",
                "ChestMoneyMedium#A4_A3ChestMoneyMediumMapPowerup#PU_Money");

            AddKeyAlias(53, "PowerupInteractable", "Dream Lands",
                "Pearl of Dreams#A4_DreamStone#PU_Stone_Dreams");
            AddKeyAlias(53, "PowerupInteractable", "A4_DreamStone",
                "Pearl of Dreams#A4_DreamStone#PU_Stone_Dreams");

            AddKeyAlias(46, "PowerupInteractable", "Dream Lands",
                "ChestMoneyExtraLarge#A4_A6ChestMoneyExtraLargeMapPowerup#PU_Money");
            AddKeyAlias(46, "PowerupInteractable", "A4_A6",
                "ChestMoneyExtraLarge#A4_A6ChestMoneyExtraLargeMapPowerup#PU_Money");

            AddKeyAlias(47, "PowerupInteractable", "Dream Lands",
                "ChestMoney1000 (2)#A4_B1ChestMoney1000 (2)MapPowerup#PU_Money");
            AddKeyAlias(47, "PowerupInteractable", "A4_B1",
                "ChestMoney1000 (2)#A4_B1ChestMoney1000 (2)MapPowerup#PU_Money");

            AddKeyAlias(48, "PowerupInteractable", "Dream Lands",
                "ChestMoney1000 (4)#A4_B1ChestMoney1000 (4)MapPowerup#PU_Money");
            AddKeyAlias(48, "PowerupInteractable", "A4_B1",
                "ChestMoney1000 (4)#A4_B1ChestMoney1000 (4)MapPowerup#PU_Money");

            AddKeyAlias(49, "PowerupInteractable", "Dream Lands",
                "ChestMoney1000 (3)#A4_B1ChestMoney1000 (3)MapPowerup#PU_Money");
            AddKeyAlias(49, "PowerupInteractable", "A4_B1",
                "ChestMoney1000 (3)#A4_B1ChestMoney1000 (3)MapPowerup#PU_Money");

            AddKeyAlias(50, "PowerupInteractable", "Dream Lands",
                "ChestMoneyLarge#A4_B1ChestMoneyLargeMapPowerup#PU_Money");
            AddKeyAlias(50, "PowerupInteractable", "A4_B1",
                "ChestMoneyLarge#A4_B1ChestMoneyLargeMapPowerup#PU_Money");

            AddKeyAlias(51, "PowerupInteractable", "Dream Lands",
                "ChestMoney1000 (1)#A4_B1ChestMoney1000 (1)MapPowerup#PU_Money");
            AddKeyAlias(51, "PowerupInteractable", "A4_B1",
                "ChestMoney1000 (1)#A4_B1ChestMoney1000 (1)MapPowerup#PU_Money");

            AddKeyAlias(52, "PowerupInteractable", "Dream Lands",
                "ChestDandaraArrow#A4_B4HealthFlask#PU_DandaraArrow");
            AddKeyAlias(52, "PowerupInteractable", "DL",
                "ChestDandaraArrow#A4_B4HealthFlask#PU_DandaraArrow");
            AddKeyAlias(52, "PowerupInteractable", "A4_B4",
                "ChestDandaraArrow#A4_B4HealthFlask#PU_DandaraArrow");
        }

        public static bool TryGetLocationId(string key, out long locationId)
        {
            return LocationIdByKey.TryGetValue(key, out locationId);
        }

        public static bool TryGetLocationIdByName(string name, out long locationId)
        {
            return LocationIdByName.TryGetValue(Normalize(name), out locationId);
        }

        public static long[] GetAllLocationIds()
        {
            long[] ids = new long[Locations.Count];
            for (int i = 0; i < Locations.Count; i++)
                ids[i] = Locations[i].Id;

            return ids;
        }

        public static bool TryGetLocationNameById(long locationId, out string locationName)
        {
            for (int i = 0; i < Locations.Count; i++)
            {
                if (Locations[i].Id == locationId)
                {
                    locationName = Locations[i].Name;
                    return true;
                }
            }

            locationName = null;
            return false;
        }

        public static bool TryResolveLocationId(string sourceType, string sceneName, string objectName,
            out long locationId)
        {
            string normalizedObject = Normalize(objectName);
            string normalizedScene = Normalize(sceneName);

            if (LocationIdByName.TryGetValue(normalizedObject, out locationId))
                return true;

            if (LocationIdByRuntimeObject.TryGetValue(normalizedObject, out locationId))
                return true;

            foreach (KeyValuePair<string, long> runtimeAlias in LocationIdByRuntimeObject)
            {
                if (normalizedObject.IndexOf(runtimeAlias.Key) >= 0)
                {
                    locationId = runtimeAlias.Value;
                    return true;
                }
            }

            for (int i = 0; i < Locations.Count; i++)
            {
                LocationEntry entry = Locations[i];
                if (normalizedObject.IndexOf(entry.NormalizedName) >= 0)
                {
                    locationId = entry.Id;
                    return true;
                }
            }

            string wantedType = GuessType(sourceType);
            if (wantedType.Length == 0)
                wantedType = GuessType(objectName);

            if (TryResolveRoomLocation(wantedType, normalizedScene, out locationId))
                return true;

            int number = FindNumber(objectName);
            if (number >= 0)
            {
                for (int i = 0; i < Locations.Count; i++)
                {
                    LocationEntry entry = Locations[i];
                    if (entry.Number == number && entry.Type == wantedType &&
                        (normalizedScene.Length == 0 || entry.NormalizedName.IndexOf(normalizedScene) >= 0 ||
                         normalizedScene.IndexOf(entry.NormalizedName) >= 0))
                    {
                        locationId = entry.Id;
                        return true;
                    }
                }

                LocationEntry fallback = null;
                bool ambiguous = false;
                for (int i = 0; i < Locations.Count; i++)
                {
                    LocationEntry entry = Locations[i];
                    if (entry.Number == number && entry.Type == wantedType)
                    {
                        if (fallback == null)
                            fallback = entry;
                        else
                            ambiguous = true;
                    }
                }

                if (fallback != null && !ambiguous)
                {
                    locationId = fallback.Id;
                    return true;
                }
            }

            locationId = 0;
            return false;
        }

        public static long[] GetLocationIdsForRoomType(string roomName, string type)
        {
            string normalizedRoom = Normalize(roomName);
            string wantedType = GuessType(type);
            if (wantedType.Length == 0)
                wantedType = type;

            List<long> ids = new List<long>();
            for (int i = 0; i < Locations.Count; i++)
            {
                LocationEntry entry = Locations[i];
                if (entry.Type == wantedType && entry.NormalizedRoomName == normalizedRoom)
                    ids.Add(entry.Id);
            }

            return ids.ToArray();
        }

        private static void Add(long id, string name)
        {
            var entry = new LocationEntry();
            entry.Id = id;
            entry.Name = name;
            entry.NormalizedName = Normalize(name);
            entry.NormalizedRoomName = Normalize(GetRoomName(name));
            entry.Type = GuessType(name);
            entry.Number = FindNumber(name);

            Locations.Add(entry);
            LocationIdByName[entry.NormalizedName] = id;
        }

        private static void AddRuntimeAlias(long id, string runtimeObject)
        {
            LocationIdByRuntimeObject[Normalize(runtimeObject)] = id;
        }

        private static void AddKeyAlias(long id, string sourceType, string sceneName, string objectName)
        {
            LocationIdByKey[LocationKey.Build(sourceType, sceneName, objectName)] = id;
        }

        private static bool TryResolveRoomLocation(string wantedType, string normalizedRoom, out long locationId)
        {
            if (wantedType.Length == 0 || normalizedRoom.Length == 0)
            {
                locationId = 0;
                return false;
            }

            LocationEntry match = null;
            for (int i = 0; i < Locations.Count; i++)
            {
                LocationEntry entry = Locations[i];
                if (entry.Type == wantedType && entry.NormalizedRoomName == normalizedRoom)
                {
                    if (match != null)
                    {
                        locationId = 0;
                        return false;
                    }

                    match = entry;
                }
            }

            if (match != null)
            {
                locationId = match.Id;
                return true;
            }

            locationId = 0;
            return false;
        }

        private static string GetRoomName(string locationName)
        {
            if (string.IsNullOrEmpty(locationName))
                return "";

            int marker = locationName.IndexOf(" (");
            if (marker > 0)
                return locationName.Substring(0, marker);

            return locationName;
        }

        private static string GuessType(string value)
        {
            string normalized = Normalize(value);
            if (normalized.IndexOf("chest") >= 0)
                return "Chest";
            if (normalized.IndexOf("altar") >= 0 || normalized.IndexOf("alter") >= 0 ||
                normalized.IndexOf("weaponaltar") >= 0)
                return "Altar";
            if (normalized.IndexOf("npc") >= 0 || normalized.IndexOf("dialogue") >= 0)
                return "NPC";
            if (normalized.IndexOf("shop") >= 0)
                return "Shop";
            return "";
        }

        private static void AddShopLocations(long firstId, string prefix, int count)
        {
            for (int i = 1; i <= count; i++)
                Add(firstId + i - 1, prefix + i);
        }

        private static int FindNumber(string value)
        {
            if (string.IsNullOrEmpty(value))
                return -1;

            int end = -1;
            for (int i = value.Length - 1; i >= 0; i--)
            {
                if (value[i] >= '0' && value[i] <= '9')
                {
                    end = i;
                    break;
                }
            }

            if (end < 0)
                return -1;

            int start = end;
            while (start > 0 && value[start - 1] >= '0' && value[start - 1] <= '9')
                start--;

            int number;
            return int.TryParse(value.Substring(start, end - start + 1), out number) ? number : -1;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";

            return value.ToLower()
                .Replace(" ", "")
                .Replace("'", "")
                .Replace("-", "")
                .Replace("_", "")
                .Replace("#", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("ú", "u")
                .Replace("Ãº", "u")
                .Replace("altar", "alter");
        }

        private class LocationEntry
        {
            public long Id;
            public string Name;
            public string NormalizedName;
            public string NormalizedRoomName;
            public int Number;
            public string Type;
        }
    }
}

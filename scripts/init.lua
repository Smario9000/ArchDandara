-- Dandara Randomizer Tracker startup.
-- Most pack data is JSON; Lua is used for helper logic and Archipelago auto-tracking.

ScriptHost:LoadScript("scripts/ap_mapping.lua")
ScriptHost:LoadScript("scripts/logic.lua")
ScriptHost:LoadScript("scripts/autotracking.lua")

Tracker:AddItems("items/items.json")
Tracker:AddMaps("maps/maps.json")
Tracker:AddLocations("locations/locations.json")
Tracker:AddLayouts("layouts/tracker.json")

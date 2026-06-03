-- Archipelago auto-tracking for Dandara Randomizer Tracker.
-- PopTracker provides the Archipelago object when the manifest variant has the "ap" flag.

local COUNT_ITEMS = { ["arrow_freedom"] = true, ["dandara_arrow_damage_upgrade"] = true, ["dandara_weapon_damage_upgrade"] = true, ["essence_salt"] = true, ["essence_salt_enhancer"] = true, ["heart_great_salt"] = true, ["infusion_salt"] = true, ["infusion_salt_enhancer"] = true, ["pleas_salt"] = true, ["pleas_salt_fear"] = true, ["salts_awareness_upgrade"] = true, ["scarf_freedom"] = true }

DANDARA_SLOT_DATA = {
    valid = false,
    block_autotracking = false,
    player_name = nil,
    player_game = "Dandara",
    goal_type = 0,
    death_link = 0,
    ammo_mana_cost = 0,
    shop_cost = 0,
    salt_drop_multiplier = 100,
    death_recovery_percent = 100,
    ap_salt_amount = 100,
    ap_fear_salt_amount = 100,
    dandara_arrow_damage_upgrade_amount = 0,
    dandara_arrow_damage_upgrade_scale = 10,
    dandara_weapon_damage_upgrade_amount = 0,
    dandara_weapon_damage_upgrade_scale = 10,
    salts_awareness_upgrade = 0,
    salts_awareness_cost_reduction = 25,
    bought_color = 0,
    received_color = 0,
    received_only_color = 0,
}

local DANDARA_OPTION_KEYS = {
    "goal_type",
    "death_link",
    "ammo_mana_cost",
    "shop_cost",
    "salt_drop_multiplier",
    "death_recovery_percent",
    "ap_salt_amount",
    "ap_fear_salt_amount",
    "dandara_arrow_damage_upgrade_amount",
    "dandara_arrow_damage_upgrade_scale",
    "dandara_weapon_damage_upgrade_amount",
    "dandara_weapon_damage_upgrade_scale",
    "salts_awareness_upgrade",
    "salts_awareness_cost_reduction",
    "bought_color",
    "received_color",
    "received_only_color",
    "custom_bought_color_r",
    "custom_bought_color_g",
    "custom_bought_color_b",
    "custom_received_color_r",
    "custom_received_color_g",
    "custom_received_color_b",
    "custom_received_only_color_r",
    "custom_received_only_color_g",
    "custom_received_only_color_b",
}

local function set_tracker_item_stage(code, stage)
    local item = Tracker:FindObjectForCode(code)
    if item then
        item.CurrentStage = stage
    end
end

local function reset_setting_items()
    set_tracker_item_stage("setting_goal", 0)
    set_tracker_item_stage("setting_ammo_mana_cost", 0)
    set_tracker_item_stage("setting_shop_cost", 0)
end

local function sync_slot_settings()
    local goal_type = tonumber(DANDARA_SLOT_DATA.goal_type) or 0
    set_tracker_item_stage("setting_goal", goal_type == 1 and 1 or 0)

    local ammo_mana_cost = tonumber(DANDARA_SLOT_DATA.ammo_mana_cost) or 0
    if ammo_mana_cost < 0 or ammo_mana_cost > 3 then
        ammo_mana_cost = 0
    end
    set_tracker_item_stage("setting_ammo_mana_cost", ammo_mana_cost)

    local shop_cost = tonumber(DANDARA_SLOT_DATA.shop_cost) or 0
    if shop_cost < 0 or shop_cost > 6 then
        shop_cost = 0
    end
    set_tracker_item_stage("setting_shop_cost", shop_cost)
end

local function is_dandara_slot_data(slot_data)
    if type(slot_data) ~= "table" then
        return false
    end

    local game = slot_data.player_game or slot_data.game or slot_data.PlayerGame
    if game ~= nil and game ~= "Dandara" then
        return false
    end

    return slot_data.player_name ~= nil
        or slot_data.PlayerName ~= nil
        or slot_data.goal_type ~= nil
        or slot_data.ammo_mana_cost ~= nil
end

local function sync_slot_data(slot_data)
    if type(slot_data) == "table" then
        local game = slot_data.player_game or slot_data.game or slot_data.PlayerGame
        if game ~= nil and game ~= "Dandara" then
            DANDARA_SLOT_DATA.valid = false
            DANDARA_SLOT_DATA.block_autotracking = true
            reset_setting_items()
            print("Dandara AP: blocking non-Dandara slot data for game " .. tostring(game))
            return false
        end
    end

    if not is_dandara_slot_data(slot_data) then
        DANDARA_SLOT_DATA.valid = false
        DANDARA_SLOT_DATA.block_autotracking = false
        reset_setting_items()
        print("Dandara AP: no Dandara slot data found; continuing with tracker defaults")
        return false
    end

    DANDARA_SLOT_DATA.valid = true
    DANDARA_SLOT_DATA.block_autotracking = false
    DANDARA_SLOT_DATA.player_name = slot_data.player_name or slot_data.PlayerName
    DANDARA_SLOT_DATA.player_game = slot_data.player_game or slot_data.game or slot_data.PlayerGame or "Dandara"

    for _, key in ipairs(DANDARA_OPTION_KEYS) do
        if slot_data[key] ~= nil then
            DANDARA_SLOT_DATA[key] = slot_data[key]
        end
    end

    sync_slot_settings()
    print("Dandara AP: slot data loaded for " .. tostring(DANDARA_SLOT_DATA.player_name) .. " (" .. tostring(DANDARA_SLOT_DATA.player_game) .. "), goal_type=" .. tostring(DANDARA_SLOT_DATA.goal_type))
    return true
end

function dandara_goal_type()
    return tonumber(DANDARA_SLOT_DATA.goal_type) or 0
end

function dandara_goal_is_true_ending()
    return dandara_goal_type() == 1
end

function dandara_slot_option(option_name, default)
    local value = DANDARA_SLOT_DATA[option_name]
    if value == nil then
        return default
    end
    return value
end

local function find_tracker_item(code)
    local obj = Tracker:FindObjectForCode(code)
    if not obj then
        print("Dandara AP: tracker item code not found: " .. code)
    end
    return obj
end

local function find_location_section(mapping)
    local obj = Tracker:FindObjectForCode(mapping.ref)
    if not obj then
        print("Dandara AP: tracker location not found: " .. mapping.code .. " -> " .. mapping.ref)
    end
    return obj
end

local function sync_shop_chain_state()
    if not AP_SHOP_CHAIN_MAP then
        return
    end

    for _, mapping in pairs(AP_SHOP_CHAIN_MAP) do
        local section = Tracker:FindObjectForCode(mapping.ref)
        local item = Tracker:FindObjectForCode(mapping.code)
        if section and item then
            item.Active = section.AvailableChestCount == 0
        end
    end
end

local function sync_location_mirrors()
    for _, mapping in pairs(AP_LOCATION_MAP) do
        if mapping.mirror then
            local section = Tracker:FindObjectForCode(mapping.ref)
            local item = Tracker:FindObjectForCode(mapping.mirror)
            if section and item then
                item.Active = section.AvailableChestCount == 0
            end
        end
    end
end

function reset_dandara_tracker()
    Tracker.BulkUpdate = true
    local ok, err = pcall(function()
        for _, code in pairs(AP_ITEM_MAP) do
            local obj = Tracker:FindObjectForCode(code)
            if obj then
                if COUNT_ITEMS[code] then
                    obj.AcquiredCount = 0
                else
                    obj.Active = false
                end
            end
        end

        for _, code in pairs(AP_EVENT_ITEM_MAP or {}) do
            local obj = Tracker:FindObjectForCode(code)
            if obj then
                obj.Active = false
            end
        end

        for _, mapping in pairs(AP_SHOP_CHAIN_MAP or {}) do
            local obj = Tracker:FindObjectForCode(mapping.code)
            if obj then
                obj.Active = false
            end
        end

        for _, mapping in pairs(AP_LOCATION_MAP) do
            local obj = Tracker:FindObjectForCode(mapping.ref)
            if obj then
                obj.AvailableChestCount = obj.ChestCount
            end

            if mapping.mirror then
                local mirror = Tracker:FindObjectForCode(mapping.mirror)
                if mirror then
                    mirror.Active = false
                end
            end
        end
    end)
    Tracker.BulkUpdate = false

    if not ok then
        print("Dandara AP: reset failed: " .. tostring(err))
    end

    sync_shop_chain_state()
    sync_location_mirrors()
end

function on_clear(slot_data)
    print("Dandara AP: clearing tracker state")
    sync_slot_data(slot_data)
    reset_dandara_tracker()
end

function on_item(index, item_id, item_name, player_number)
    if DANDARA_SLOT_DATA.block_autotracking then
        print("Dandara AP: ignoring item for non-Dandara slot: " .. tostring(item_name))
        return
    end

    local code_from_name = AP_ITEM_MAP[item_name]
    local code_from_id = AP_ITEM_ID_MAP and AP_ITEM_ID_MAP[item_id] or nil
    local code = code_from_id or code_from_name

    if not code then
        print("Dandara AP: unmapped item: " .. tostring(item_name) .. " id=" .. tostring(item_id))
        return
    end

    if code_from_name and code_from_id and code_from_name ~= code_from_id then
        print("Dandara AP: item name/id mismatch, using id mapping: name=" .. tostring(item_name) .. " id=" .. tostring(item_id))
    end

    local obj = find_tracker_item(code)
    if not obj then
        return
    end

    if COUNT_ITEMS[code] then
        obj.AcquiredCount = obj.AcquiredCount + 1
    else
        obj.Active = true
    end
end

function on_location(location_id, location_name)
    if DANDARA_SLOT_DATA.block_autotracking then
        print("Dandara AP: ignoring location for non-Dandara slot: " .. tostring(location_name))
        return
    end

    local mapping = AP_LOCATION_MAP[location_name]
    if not mapping then
        print("Dandara AP: unmapped location: " .. tostring(location_name))
        return
    end

    local obj = find_location_section(mapping)
    if not obj then
        return
    end

    obj.AvailableChestCount = 0
    sync_shop_chain_state()
    sync_location_mirrors()

    -- Some APWorld locations are local event locations that grant an internal
    -- progression flag. If such a location is ever checked through the AP
    -- handler, mirror that event flag for later access rules.
    if mapping.event then
        local event_obj = Tracker:FindObjectForCode(mapping.event)
        if event_obj then
            event_obj.Active = true
        end
    end
end

if ScriptHost and ScriptHost.AddOnLocationSectionChangedHandler then
    ScriptHost:AddOnLocationSectionChangedHandler("Dandara Shop Chain", function(section)
        sync_shop_chain_state()
        sync_location_mirrors()
    end)
end

if Archipelago then
    Archipelago:AddClearHandler("Dandara Clear", on_clear)
    Archipelago:AddItemHandler("Dandara Item", on_item)
    Archipelago:AddLocationHandler("Dandara Location", on_location)
else
    print("Dandara AP: Archipelago API not available. Check that manifest.json enables the ap flag.")
end

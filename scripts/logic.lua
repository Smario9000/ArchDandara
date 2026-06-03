-- Logic placeholder for the first tracker version.
-- Add PopTracker access rule functions here as the Dandara logic model is ported from the APWorld.

function has(code, amount)
    amount = amount or 1
    return Tracker:ProviderCountForCode(code) >= amount
end

-- Shop upgrades are a single APWorld chain named Buy Upgrade 1..41.
-- This checks the previous location section directly, so both AP auto-tracking
-- and manual clicks unlock the next shop upgrade.
function shop_upgrade_bought(upgrade_number)
    local number = tonumber(upgrade_number)
    if not number then
        return false
    end

    local mapping = AP_SHOP_CHAIN_MAP and AP_SHOP_CHAIN_MAP["Buy Upgrade " .. number]
    if not mapping then
        return false
    end

    local section = Tracker:FindObjectForCode(mapping.ref)
    return section ~= nil and section.AvailableChestCount == 0
end

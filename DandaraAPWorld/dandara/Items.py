# ArchDandara documentation
# Purpose: Defines AP item classifications, counts, filler, and dynamic upgrade item generation.
# Why: The generated item pool must match the mod item grant table so AP placements produce valid in-game effects.
# Notes: Item counts here drive total pool size; changing progression counts affects filler amount and logic balance.

from BaseClasses import ItemClassification
from .Locations import *
from .Options import *

# File is Auto-generated, see: [https://github.com/SWCreeperKing/ApWorldFactories/tree/master/ApWorldFactories/Games]

progression_items = {
	# Movement progression has extra copies to reduce early multiworld bottlenecks. The mod
	# converts duplicate movement copies into Pleas of the Salt after one is already owned.
	"FearKey": 1,
	"FreeNara": 1,
	"TimeFlag": 1,
	"Boss StoryEvent Key 1": 1,
	"Boss StoryEvent Key 2": 1,
	"Stone of Creation": 3,
	"Rock of Remembrance": 1,
	"Stone of Intention": 1,
	"Pearl of Dreams": 1,
	"Shell Mirror": 1,
	"Heart of the Great Salt": 17,
	"Scarf of Freedom": 9,
	"Essence of Salt": 9,
	"Infusion of Salt": 9,
	"Essence of Salt Enhancer": 6,
	"Infusion of Salt Enhancer": 6,
	"Jonny B. Missiles": 1,
	"Anxiety Shock": 1,
	"Memories Shaft": 1,
	"Logic Blast": 1,
	"Skin Knitter": 1,
	"Displaced Presence": 3,
	"Paint Platform": 1,
	"Music Platform": 1,
	"DLC StoryEvent 1": 1,
	"DLC StoryEvent 2": 1,
	"DLC StoryEvent 3": 1,
	"DLC StoryEvent 4": 1,
	"DLC StoryEvent 5": 1,
	"DLC StoryEvent 6": 1,
	"DLC StoryEvent 7": 1,
	"Wall Break": 1,
	"Essence Enhancer Permit": 1,
	"Infusion Enhancer Permit": 1,
	"Freedom Enhancer Permit": 1,
	"Heart Enhancer Permit": 1
}

useful_items = {
	"Map": 1,
	"Bracers of the Patient": 1,
	"Arrow of Freedom": 1,
	"Salt's Awareness": 1
}

upgrade_items = {
	"Dandara Arrow Damage Upgrade": 0,
	"Dandara Weapon Damage Upgrade": 0,
	"Salt's Awareness Upgrade": 0
}

filler_items = {
	"Pleas of the Salt Fear": 24,
	"Pleas of the Salt": 44
}

item_table = {
	**{item: ItemClassification.progression for item in progression_items},
	**{item: ItemClassification.useful for item in useful_items},
	**{item: ItemClassification.useful for item in upgrade_items},
	**{item: ItemClassification.filler for item in filler_items},
	"Salt": ItemClassification.filler
}

raw_items = [item for item, classification in item_table.items()]

def gen_create_items(world):
	pool = world.multiworld.itempool
	options = world.options
	dynamic_upgrades = get_dynamic_upgrade_counts(options)
	# Every non-filler item reduces location_count. Whatever remains after progression,
	# useful, dynamic upgrades, and fixed filler is filled with generic Salt.
	for item, amt in progression_items.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	for item, amt in useful_items.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	for item, amt in dynamic_upgrades.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	remaining_filler_items = reduce_filler_counts(sum(dynamic_upgrades.values()))
	# Dynamic upgrade items replace stronger filler first so the total item pool size still
	# matches the number of AP locations.
	for item, amt in remaining_filler_items.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	for _ in range(world.location_count):
		pool.append(world.create_item("Salt"))

def get_dynamic_upgrade_counts(options):
	return {
		"Dandara Arrow Damage Upgrade": options.dandara_arrow_damage_upgrade_amount.upgrade_count(),
		"Dandara Weapon Damage Upgrade": options.dandara_weapon_damage_upgrade_amount.upgrade_count(),
		"Salt's Awareness Upgrade": int(options.salts_awareness_upgrade.value)
	}

def reduce_filler_counts(amount_to_remove):
	counts = dict(filler_items)
	for item in ("Pleas of the Salt", "Pleas of the Salt Fear"):
		if amount_to_remove <= 0:
			break
		removed = min(counts[item], amount_to_remove)
		counts[item] -= removed
		amount_to_remove -= removed
	return counts

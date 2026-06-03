# ArchDandara documentation
# Purpose: Defines AP item classifications, counts, filler, and dynamic upgrade item generation.
# Why: The generated item pool must match the mod item grant table so AP placements produce valid in-game effects.
# Notes: Item counts here drive total pool size; changing progression counts affects filler amount and logic balance.

from BaseClasses import ItemClassification
from .Locations import *
from .Options import *

# File is Auto-generated, see: [https://github.com/SWCreeperKing/ApWorldFactories/tree/master/ApWorldFactories/Games]

base_progression_items = {
	# These are the always-present progression counts. Items with duplicate safety copies
	# are expanded in get_progression_item_counts so goal-specific pools stay explicit.
	"FearKey": 1,
	"TimeFlag": 1,
	"Boss StoryEvent Key 1": 1,
	"Boss StoryEvent Key 2": 1,
	"Shell Mirror": 1,
	"Heart of the Great Salt": 17,
	"Scarf of Freedom": 9,
	"Essence of Salt": 9,
	"Infusion of Salt": 9,
	"Essence of Salt Enhancer": 6,
	"Infusion of Salt Enhancer": 6,
	"Displaced Presence": 1,
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

four_chance_progression_items = {
	"Stone of Creation": 4,
	"Rock of Remembrance": 4,
	"Stone of Intention": 4,
	"Pearl of Dreams": 4,
	"Jonny B. Missiles": 4,
	"Anxiety Shock": 4,
	"Memories Shaft": 4,
	"Logic Blast": 4,
	"Skin Knitter": 4,
	"Paint Platform": 4,
	"Music Platform": 4
}

three_chance_permit_items = {
	"Essence Enhancer Permit": 3,
	"Infusion Enhancer Permit": 3,
	"Freedom Enhancer Permit": 3,
	"Heart Enhancer Permit": 3
}

five_chance_true_ending_items = {
	"FearKey": 5,
	"Shell Mirror": 5,
	"FreeNara": 5
}

base_useful_items = {
	"Arrow of Freedom": 6
}

five_chance_useful_items = {
	"Map": 5,
	"Bracers of the Patient": 5,
	"Salt's Awareness": 5
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
	**{item: ItemClassification.progression for item in base_progression_items},
	**{item: ItemClassification.progression for item in four_chance_progression_items},
	**{item: ItemClassification.progression for item in three_chance_permit_items},
	**{item: ItemClassification.progression for item in five_chance_true_ending_items},
	**{item: ItemClassification.useful for item in base_useful_items},
	**{item: ItemClassification.useful for item in five_chance_useful_items},
	**{item: ItemClassification.useful for item in upgrade_items},
	**{item: ItemClassification.filler for item in filler_items},
	"Salt": ItemClassification.filler
}

raw_items = [item for item, classification in item_table.items()]

def gen_create_items(world):
	pool = world.multiworld.itempool
	options = world.options
	dynamic_upgrades = get_dynamic_upgrade_counts(options)
	progression_counts = get_progression_item_counts(options)
	useful_counts = get_useful_item_counts()
	duplicate_extra_count = get_duplicate_extra_count(options)
	# Every non-filler item reduces location_count. Whatever remains after progression,
	# useful, dynamic upgrades, and fixed filler is filled with generic Salt.
	for item, amt in progression_counts.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	for item, amt in useful_counts.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	for item, amt in dynamic_upgrades.items():
		world.location_count -= amt
		for _ in range(amt):
			pool.append(world.create_item(item))
	remaining_filler_items = reduce_filler_counts(sum(dynamic_upgrades.values()) + duplicate_extra_count)
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

def get_progression_item_counts(options):
	counts = dict(base_progression_items)
	counts.update(four_chance_progression_items)
	counts.update(three_chance_permit_items)
	if int(options.goal_type.value) == GoalType.option_true_ending:
		counts.update(five_chance_true_ending_items)
	return counts

def get_useful_item_counts():
	counts = dict(base_useful_items)
	counts.update(five_chance_useful_items)
	return counts

def get_duplicate_extra_count(options):
	# Extra AP copies are intentional safety copies. The mod turns copies beyond the
	# usable limit into salt, so the APWorld removes the same amount from filler.
	extra = sum(amt - 1 for amt in four_chance_progression_items.values())
	extra += sum(amt - 1 for amt in three_chance_permit_items.values())
	extra += sum(amt - 1 for amt in five_chance_useful_items.values())
	extra += base_useful_items["Arrow of Freedom"] - 3
	if int(options.goal_type.value) == GoalType.option_true_ending:
		extra += sum(amt - 1 for amt in five_chance_true_ending_items.values())
	return extra

def reduce_filler_counts(amount_to_remove):
	counts = dict(filler_items)
	for item in ("Pleas of the Salt", "Pleas of the Salt Fear"):
		if amount_to_remove <= 0:
			break
		removed = min(counts[item], amount_to_remove)
		counts[item] -= removed
		amount_to_remove -= removed
	return counts

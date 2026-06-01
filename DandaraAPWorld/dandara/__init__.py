# ArchDandara documentation
# Purpose: Registers the Dandara APWorld, creates regions and items, fills slot data, and handles generation output.
# Why: This is the AP server-side partner to the mod, so it exports the data the game client needs at runtime.
# Notes: Slot data exported here is the contract consumed by the mod after connecting to the AP server.

from worlds.AutoWorld import World
from .Locations import *
from .Rules import *
from .Options import *
from .Items import *
from .Regions import *
from .Settings import *
from typing import *

# File is Auto-generated, see: [https://github.com/SWCreeperKing/ApWorldFactories/tree/master/ApWorldFactories/Games]

class Dandara(World):
	"""
	Dandara
	"""
	game = "Dandara"
	options_dataclass = DandaraOptions
	options: DandaraOptions
	settings: ClassVar[DandaraSettings]
	topology_present = True
	ut_can_gen_without_yaml = True
	gen_puml = False
	location_name_to_id = {value: location_dict.index(value) + 1 for value in location_dict}
	item_name_to_id = {value: raw_items.index(value) + 1 for value in raw_items}

	def __init__(self, multiworld: "MultiWorld", player: int):
		super().__init__(multiworld, player)
		self.location_count = 0

	def generate_early(self):
		options = self.options
		if hasattr(self.multiworld, "re_gen_passthrough"):
			# Universal Tracker and regeneration flows can pass option values directly. Respecting
			# passthrough here keeps tracker/test generation aligned with normal yaml generation.
			if "Dandara" not in self.multiworld.re_gen_passthrough: return
			passthrough = self.multiworld.re_gen_passthrough["Dandara"]
			if "goal_type" in passthrough:
				options.goal_type = GoalType(passthrough["goal_type"])
			
		check_options(self)
		self._reserve_early_movement()

	def _reserve_early_movement(self):
		# Reserve one of the two main movement items as early across the multiworld. This is not
		# local-only, so another player can still hold it while preventing long movement BK chains.
		early_movement_item = self.random.choice(("Stone of Creation", "Displaced Presence"))
		early_items = self.multiworld.early_items[self.player]
		early_items[early_movement_item] = max(early_items.get(early_movement_item, 0), 1)

	def create_regions(self):
		gen_create_regions(self)

	def create_item(self, name: str):
		return Item(name, item_table[name], self.item_name_to_id[name], self.player)

	def create_items(self):
		gen_create_items(self)

	def set_rules(self):
		player = self.player
		options = self.options
		match options.goal_type:
			case 0:
				self.multiworld.completion_condition[self.player] = lambda state: has(state, player, options, "FinalBoss_Kill")
			case 1:
				self.multiworld.completion_condition[self.player] = lambda state: has(state, player, options, "DLCF_FearEnded")
		
	def fill_slot_data(self):
		options = self.options
		# Slot data is the runtime contract with the MelonLoader mod. Keep keys stable unless the
		# mod-side APSlotSettings parser is updated at the same time.
		slot_data = {
			"player_name": self.player_name,
			"goal_type": int(options.goal_type.value),
			"death_link": int(options.death_link.value),
			"ammo_mana_cost": int(options.ammo_mana_cost.value),
			"shop_cost": int(options.shop_cost.value),
			"salt_drop_multiplier": int(options.salt_drop_multiplier.value),
			"death_recovery_percent": int(options.death_recovery_percent.value),
			"ap_salt_amount": int(options.ap_salt_amount.value),
			"ap_fear_salt_amount": int(options.ap_fear_salt_amount.value),
			"dandara_arrow_damage_upgrade_amount": options.dandara_arrow_damage_upgrade_amount.upgrade_count(),
			"dandara_arrow_damage_upgrade_scale": int(options.dandara_arrow_damage_upgrade_scale.value),
			"dandara_weapon_damage_upgrade_amount": options.dandara_weapon_damage_upgrade_amount.upgrade_count(),
			"dandara_weapon_damage_upgrade_scale": int(options.dandara_weapon_damage_upgrade_scale.value),
			"salts_awareness_upgrade": int(options.salts_awareness_upgrade.value),
			"salts_awareness_cost_reduction": int(options.salts_awareness_cost_reduction.value),
			"bought_color": int(options.bought_color.value),
			"received_color": int(options.received_color.value),
			"received_only_color": int(options.received_only_color.value),
			"boss_key_hints": self._get_boss_key_hints(),
			"item_location_hints": self._get_item_location_hints(),
		}

		if int(options.bought_color.value) == 5:
			# Custom RGB values are only exported when Custom is selected. This keeps generated
			# spoiler/settings output readable for players using preset color schemes.
			slot_data["custom_bought_color_r"] = options.custom_bought_color_r.get_value("custom_bought_color_set_r", 25)
			slot_data["custom_bought_color_g"] = options.custom_bought_color_g.get_value("custom_bought_color_set_g", 140)
			slot_data["custom_bought_color_b"] = options.custom_bought_color_b.get_value("custom_bought_color_set_b", 255)

		if int(options.received_color.value) == 5:
			slot_data["custom_received_color_r"] = options.custom_received_color_r.get_value("custom_received_color_set_r", 180)
			slot_data["custom_received_color_g"] = options.custom_received_color_g.get_value("custom_received_color_set_g", 50)
			slot_data["custom_received_color_b"] = options.custom_received_color_b.get_value("custom_received_color_set_b", 255)

		if int(options.received_only_color.value) == 5:
			slot_data["custom_received_only_color_r"] = options.custom_received_only_color_r.get_value("custom_received_only_color_set_r", 255)
			slot_data["custom_received_only_color_g"] = options.custom_received_only_color_g.get_value("custom_received_only_color_set_g", 50)
			slot_data["custom_received_only_color_b"] = options.custom_received_only_color_b.get_value("custom_received_only_color_set_b", 180)

		return slot_data

	def _get_boss_key_hints(self):
		return self._get_hints_for_items(("Boss StoryEvent Key 1", "Boss StoryEvent Key 2"))

	def _get_item_location_hints(self):
		return self._get_hints_for_items((
			"DLC StoryEvent 1",
			"DLC StoryEvent 2",
			"DLC StoryEvent 3",
			"DLC StoryEvent 4",
			"DLC StoryEvent 5",
			"DLC StoryEvent 6",
			"DLC StoryEvent 7",
			"TimeFlag"
		))

	def _get_hints_for_items(self, item_names):
		hints = {}
		for location in self.multiworld.get_locations():
			if location.item is None:
				continue

			if location.item.player != self.player:
				continue

			if location.item.name not in item_names:
				continue

			# These hints are free slot data, not server !hint commands. Boss gates can show useful
			# location text without spending AP hint points or requiring TextClient permissions.
			hints[location.item.name] = {
				"player": self.multiworld.get_player_name(location.player),
				"location": location.name
			}

		return hints

	def write_spoiler_header(self, spoiler_handle):
		options = self.options
		self._write_custom_color_spoiler(spoiler_handle, "Bought",
			int(options.bought_color.value),
			options.custom_bought_color_r, "custom_bought_color_set_r", 25,
			options.custom_bought_color_g, "custom_bought_color_set_g", 140,
			options.custom_bought_color_b, "custom_bought_color_set_b", 255)
		self._write_custom_color_spoiler(spoiler_handle, "Received",
			int(options.received_color.value),
			options.custom_received_color_r, "custom_received_color_set_r", 180,
			options.custom_received_color_g, "custom_received_color_set_g", 50,
			options.custom_received_color_b, "custom_received_color_set_b", 255)
		self._write_custom_color_spoiler(spoiler_handle, "Received Only",
			int(options.received_only_color.value),
			options.custom_received_only_color_r, "custom_received_only_color_set_r", 255,
			options.custom_received_only_color_g, "custom_received_only_color_set_g", 50,
			options.custom_received_only_color_b, "custom_received_only_color_set_b", 180)

	def _write_custom_color_spoiler(self, spoiler_handle, label, scheme, red_option, red_key,
			red_default, green_option, green_key, green_default, blue_option, blue_key, blue_default):
		if scheme != 5:
			return

		spoiler_handle.write(f"  Custom {label} Color R:".ljust(34) +
			f"{red_key}: {red_option.get_value(red_key, red_default)}\n")
		spoiler_handle.write(f"  Custom {label} Color G:".ljust(34) +
			f"{green_key}: {green_option.get_value(green_key, green_default)}\n")
		spoiler_handle.write(f"  Custom {label} Color B:".ljust(34) +
			f"{blue_key}: {blue_option.get_value(blue_key, blue_default)}\n")


	def generate_output(self, output_directory: str):
		if self.gen_puml: 
		    from Utils import visualize_regions
		    state = self.multiworld.get_all_state(False)
		    state.update_reachable_regions(self.player)
		    visualize_regions(self.get_region("Menu"), f"{self.player_name}_world.puml",
		                      show_entrance_names=True,
		                      regions_to_highlight=state.reachable_regions[self.player])

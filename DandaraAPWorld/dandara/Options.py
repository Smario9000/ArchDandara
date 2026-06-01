# ArchDandara documentation
# Purpose: Defines yaml options for goals, costs, colors, DeathLink, salt, and upgrade scaling.
# Why: These options let AP generation choose per-player settings that the mod reads from slot data.
# Notes: Option display text becomes generated yaml guidance, so warnings should be clear for risky logic settings.

from dataclasses import dataclass
from Options import *
from .Locations import *

# File is Auto-generated, see: [https://github.com/SWCreeperKing/ApWorldFactories/tree/master/ApWorldFactories/Games]

class GoalType(Choice):
	"""
	Which Boss to defeat as goal
	"""
	display_name = "Goal Type"
	option_final_boss = 0
	option_true_ending = 1
	default = 0

class AmmoManaCost(Choice):
	"""
	Changes the mana/ammo cost used by secondary weapons.
	No Change keeps the game's normal costs. Half Cost uses 50%. Quarter Cost uses 25%. Double Cost uses 200%.
	Double Cost is not fully represented in current AP logic; use it at your own risk.
	"""
	display_name = "Ammo/Mana Cost"
	option_no_change = 0
	option_half_cost = 1
	option_quarter_cost = 2
	option_double_cost = 3
	default = 0

class ShopCost(Choice):
	"""
	Changes shop upgrade prices.
	Normal uses the base table. Cheaper options reduce the table. More expensive options increase it.
	"""
	display_name = "Shop Cost"
	option_normal = 0
	option_15_percent_cheaper = 1
	option_25_percent_cheaper = 2
	option_64_percent_cheaper = 3
	option_15_percent_more = 4
	option_20_percent_more = 5
	option_35_percent_more = 6
	default = 0

class SaltDropMultiplier(Choice):
	"""
	Multiplier for salt dropped by the game world, as a percent.
	100 is no change, 200 is 2x, 400 is 4x. Half and Quarter are provided as named options.
	Maximum is 800, which is 8x.
	This does not affect salt items sent by Archipelago.
	"""
	display_name = "Salt Drop Multiplier"
	option_quarter = 25
	option_half = 50
	option_no_change = 100
	option_200 = 200
	option_400 = 400
	option_800 = 800
	default = 100

class DeathRecoveryPercent(Range):
	"""
	Percent of salt returned when recovering the dead player body.
	100 is normal game behavior. 80 returns 80%, 50 returns 50%, and 10 returns 10%.
	You may choose any value from 1 to 100.
	This recovery is never affected by Salt Drop Multiplier.
	"""
	display_name = "Dead Player Body Salt Recovery"
	range_start = 1
	range_end = 100
	default = 100

class APSaltAmount(Choice):
	"""
	Multiplier for normal salt items sent by Archipelago.
	"""
	display_name = "AP Salt Amount"
	option_normal_amount = 100
	option_half = 50
	option_200 = 200
	option_500 = 500
	default = 100

class APFearSaltAmount(Choice):
	"""
	Multiplier for fear salt items sent by Archipelago.
	"""
	display_name = "AP Fear Salt Amount"
	option_normal_amount = 100
	option_half = 50
	option_150 = 150
	option_300 = 300
	default = 100

class DamageUpgradeAmount(Choice):
	"""
	How many damage upgrade items are added to the item pool.
	Option 1 adds 4 upgrades. Option 2 adds 3 upgrades. Option 3 adds 2 upgrades.
	Option 4 adds 1 upgrade. Option 5 adds no upgrades.
	"""
	option_4_upgrades = 1
	option_3_upgrades = 2
	option_2_upgrades = 3
	option_1_upgrade = 4
	option_no_upgrades = 5
	default = 5

	def upgrade_count(self):
		return max(0, 5 - int(self.value))

class DandaraArrowDamageUpgradeAmount(DamageUpgradeAmount):
	display_name = "Arrow Damage Upgrade Amount"

class DandaraWeaponDamageUpgradeAmount(DamageUpgradeAmount):
	display_name = "Weapon Damage Upgrade Amount"

class DamageUpgradeScale(Range):
	"""
	Damage upgrade scale from 0.5 to 3.0. YAML may use a float like 1.5.
	Internally this is stored as percent, so 1.5 becomes 150.
	"""
	range_start = 50
	range_end = 300
	default = 100

	@classmethod
	def from_any(cls, data):
		# Archipelago Range values are stored as integers. Accepting floats keeps yaml
		# user-friendly while still sending stable percent values to the client mod.
		if isinstance(data, float):
			return cls(round(data * 100))
		if isinstance(data, str):
			text = data.strip()
			try:
				if "." in text:
					return cls(round(float(text) * 100))
			except ValueError:
				pass
		return super().from_any(data)

class DandaraArrowDamageUpgradeScale(DamageUpgradeScale):
	display_name = "Arrow Damage Upgrade Scale"

class DandaraWeaponDamageUpgradeScale(DamageUpgradeScale):
	display_name = "Weapon Damage Upgrade Scale"

class SaltsAwarenessUpgrade(Toggle):
	"""
	Add one Salt's Awareness Upgrade item to the pool.
	"""
	display_name = "Salt's Awareness Upgrade"

class SaltsAwarenessCostReduction(Range):
	"""
	Percent less salt spent over time while upgraded Salt's Awareness is active.
	5 is 5% less cost, 75 is 75% less cost.
	"""
	display_name = "Salt's Awareness Cost Reduction"
	range_start = 5
	range_end = 75
	default = 40

class DeathLink(Toggle):
	"""
	When enabled, dying in Dandara sends a DeathLink to other players, and DeathLinks from other players kill Dandara.
	Can also be toggled by the Archipelago TextClient with /deathlink.
	"""
	display_name = "DeathLink"

class ShopBarColorScheme(Choice):
	"""
	Choose the color scheme for one shop bar state.
	Use Custom to read that state's custom RGB options. RGB values are 0-255.
	"""
	display_name = "Shop Bar Color Scheme"
	option_default = 0
	option_red_blue_colorblind = 1
	option_blue_green_colorblind = 2
	option_green_red_colorblind = 3
	option_high_contrast = 4
	option_custom = 5
	default = 0

class BoughtColorScheme(ShopBarColorScheme):
	"""
	Choose the color scheme for one shop bar state.
	Use Custom to read that state's custom RGB options. RGB values are 0-255.
	"""
	display_name = "SB Bought Color Scheme"

class ReceivedColorScheme(ShopBarColorScheme):
	"""
	Choose the color scheme for one shop bar state.
	Use Custom to read that state's custom RGB options. RGB values are 0-255.
	"""
	display_name = "SB Received Color Scheme"

class ReceivedOnlyColorScheme(ShopBarColorScheme):
	"""
	Choose the color scheme for one shop bar state.
	Use Custom to read that state's custom RGB options. RGB values are 0-255.
	"""
	display_name = "SB Received Only Color Scheme"

class CustomColorChannel(OptionDict):
	"""
	Custom RGB channel value, from 0 to 255.
	Only used when the matching shop bar color option is set to Custom.
	This is a variable dictionary, not a weighted option.
	"""
	# These are variable dictionaries instead of weighted choices. When Custom is selected,
	# generation reads the exact channel value and passes it to slot_data unchanged.
	supports_weighting = False
	visibility = Visibility.template

	def get_value(self, key, default):
		value = self.value.get(key, default)
		if not isinstance(value, int):
			raise OptionError(f"{key} must be a whole number from 0 to 255.")
		if value < 0 or value > 255:
			raise OptionError(f"{key} must be between 0 and 255.")
		return value

class CustomBoughtColorR(CustomColorChannel):
	display_name = "Custom Bought Color R"
	valid_keys = ["custom_bought_color_set_r"]
	default = {"custom_bought_color_set_r": 25}

class CustomBoughtColorG(CustomColorChannel):
	display_name = "Custom Bought Color G"
	valid_keys = ["custom_bought_color_set_g"]
	default = {"custom_bought_color_set_g": 140}

class CustomBoughtColorB(CustomColorChannel):
	display_name = "Custom Bought Color B"
	valid_keys = ["custom_bought_color_set_b"]
	default = {"custom_bought_color_set_b": 255}

class CustomReceivedColorR(CustomColorChannel):
	display_name = "Custom Received Color R"
	valid_keys = ["custom_received_color_set_r"]
	default = {"custom_received_color_set_r": 180}

class CustomReceivedColorG(CustomColorChannel):
	display_name = "Custom Received Color G"
	valid_keys = ["custom_received_color_set_g"]
	default = {"custom_received_color_set_g": 50}

class CustomReceivedColorB(CustomColorChannel):
	display_name = "Custom Received Color B"
	valid_keys = ["custom_received_color_set_b"]
	default = {"custom_received_color_set_b": 255}

class CustomReceivedOnlyColorR(CustomColorChannel):
	display_name = "Custom Received Only Color R"
	valid_keys = ["custom_received_only_color_set_r"]
	default = {"custom_received_only_color_set_r": 255}

class CustomReceivedOnlyColorG(CustomColorChannel):
	display_name = "Custom Received Only Color G"
	valid_keys = ["custom_received_only_color_set_g"]
	default = {"custom_received_only_color_set_g": 50}

class CustomReceivedOnlyColorB(CustomColorChannel):
	display_name = "Custom Received Only Color B"
	valid_keys = ["custom_received_only_color_set_b"]
	default = {"custom_received_only_color_set_b": 180}


@dataclass
class DandaraOptions(PerGameCommonOptions):
	# Every field here can appear in generated Dandara.yaml. If a new option affects runtime,
	# also export it in __init__.py fill_slot_data and read it in APSlotSettings.
	goal_type: GoalType
	death_link: DeathLink
	ammo_mana_cost: AmmoManaCost
	shop_cost: ShopCost
	salt_drop_multiplier: SaltDropMultiplier
	death_recovery_percent: DeathRecoveryPercent
	ap_salt_amount: APSaltAmount
	ap_fear_salt_amount: APFearSaltAmount
	dandara_arrow_damage_upgrade_amount: DandaraArrowDamageUpgradeAmount
	dandara_arrow_damage_upgrade_scale: DandaraArrowDamageUpgradeScale
	dandara_weapon_damage_upgrade_amount: DandaraWeaponDamageUpgradeAmount
	dandara_weapon_damage_upgrade_scale: DandaraWeaponDamageUpgradeScale
	salts_awareness_upgrade: SaltsAwarenessUpgrade
	salts_awareness_cost_reduction: SaltsAwarenessCostReduction
	bought_color: BoughtColorScheme
	custom_bought_color_r: CustomBoughtColorR
	custom_bought_color_g: CustomBoughtColorG
	custom_bought_color_b: CustomBoughtColorB
	received_color: ReceivedColorScheme
	custom_received_color_r: CustomReceivedColorR
	custom_received_color_g: CustomReceivedColorG
	custom_received_color_b: CustomReceivedColorB
	received_only_color: ReceivedOnlyColorScheme
	custom_received_only_color_r: CustomReceivedOnlyColorR
	custom_received_only_color_g: CustomReceivedOnlyColorG
	custom_received_only_color_b: CustomReceivedOnlyColorB

	def get_options_map(self, option):
		match option:
			case "goal_type":
				return self.goal_type
			case "death_link":
				return self.death_link
			case "ammo_mana_cost":
				return self.ammo_mana_cost
			case "shop_cost":
				return self.shop_cost
			case "salt_drop_multiplier":
				return self.salt_drop_multiplier
			case "death_recovery_percent":
				return self.death_recovery_percent
			case "ap_salt_amount":
				return self.ap_salt_amount
			case "ap_fear_salt_amount":
				return self.ap_fear_salt_amount
			case "dandara_arrow_damage_upgrade_amount":
				return self.dandara_arrow_damage_upgrade_amount
			case "dandara_arrow_damage_upgrade_scale":
				return self.dandara_arrow_damage_upgrade_scale
			case "dandara_weapon_damage_upgrade_amount":
				return self.dandara_weapon_damage_upgrade_amount
			case "dandara_weapon_damage_upgrade_scale":
				return self.dandara_weapon_damage_upgrade_scale
			case "salts_awareness_upgrade":
				return self.salts_awareness_upgrade
			case "salts_awareness_cost_reduction":
				return self.salts_awareness_cost_reduction
			case "bought_color":
				return self.bought_color
			case "received_color":
				return self.received_color
			case "received_only_color":
				return self.received_only_color
		

def check_options(world):
	options = world.options
	random = world.random
	settings = world.settings

def raise_yaml_error(player_name, error):
	raise OptionError(f'\n\n=== Dandara YAML ERROR ===\nDandara: {player_name} {error}, PLEASE FIX YOUR YAML\n\n')

using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public class ProfileMemberResponse
{
	[JsonPropertyName("player_data")] public RawMemberPlayerData? PlayerData { get; set; }

	[JsonPropertyName("pets_data")] public MemberPetsResponse? PetsData { get; set; }

	public MemberProfileDataResponse? Profile { get; set; }

	public RawMemberEvents? Events { get; set; }

	[JsonPropertyName("jacobs_contest")] public RawJacobData? Jacob { get; set; }

	public Dictionary<string, long>? Collection { get; set; }

	[JsonPropertyName("accessory_bag_storage")]
	public RawAccessoryBagStorage? AccessoryBagSettings { get; set; }

	public JsonObject? Bestiary { get; set; }

	public RawLeveling? Leveling { get; set; }
	
	public RawItemData? ItemData { get; set; }

	public MemberCurrenciesResponse? Currencies { get; set; }
	
	public RawDungeonsResponse? Dungeons { get; set; }

	[JsonPropertyName("inventory")] public MemberInventoriesResponse? Inventories { get; set; }

	[JsonPropertyName("garden_player_data")]
	public GardenPlayerDataResponse? Garden { get; set; }
}

public class GardenPlayerDataResponse
{
	public int Copper { get; set; }

	[JsonPropertyName("larva_consumed")] public int LarvaConsumed { get; set; }
}

public class RawBestiaryResponse
{
	public RawBestiaryKills Kills { get; set; } = new();
	public Dictionary<string, int> Deaths { get; set; } = new(); 
	public RawBestiaryMilestone Milestone { get; set; } = new();
}

public class RawBestiaryKills
{
	[JsonPropertyName("last_killed_mob")]
	public string? LastKilledMob { get; set; }
	
	[JsonExtensionData]
	public Dictionary<string, int> Kills { get; set; } = new();
}

public class RawBestiaryMilestone
{
	[JsonPropertyName("last_claimed_milestone")]
	public int LastClaimedMilestone { get; set; }
}

public class RawAccessoryBagStorage {
	[JsonPropertyName("tuning")] public AccessoryBagTuningSlots? Tuning { get; set; }
	[JsonPropertyName("selected_power")] public string? SelectedPower { get; set; }
	[JsonPropertyName("highest_magical_power")] public int HighestMagicalPower { get; set; }
	[JsonPropertyName("bag_upgrades_purchased")] public int BagUpgradesPurchased { get; set; }
	[JsonPropertyName("unlocked_powers")] public List<string> UnlockedPowers { get; set; } = [];
}

public class AccessoryBagTuning {
	public int Health { get; set; }
	public int Defense { get; set; }
	[JsonPropertyName("walk_speed")]
	public int Speed { get; set; }
	public int Strength { get; set; }
	[JsonPropertyName("critical_damage")]
	public int CriticalDamage { get; set; }
	[JsonPropertyName("critical_chance")]
	public int CriticalChance { get; set; }
	public int Intelligence { get; set; }
	[JsonPropertyName("attack_speed")]
	public int AttackSpeed { get; set; }
}

public class AccessoryBagTuningSlots {
	[JsonPropertyName("slot_0")] public AccessoryBagTuning? Slot0 { get; set; }
	[JsonPropertyName("slot_1")] public AccessoryBagTuning? Slot1 { get; set; }
	[JsonPropertyName("slot_2")] public AccessoryBagTuning? Slot2 { get; set; }
	[JsonPropertyName("slot_3")] public AccessoryBagTuning? Slot3 { get; set; }
	[JsonPropertyName("slot_4")] public AccessoryBagTuning? Slot4 { get; set; }

	[JsonPropertyName("highest_unlocked_slot")] public int? HighestUnlockedSlot { get; set; }
	[JsonPropertyName("refund_1")] public bool? Refund1 { get; set; }
}

public class RawItemData {
	public long Soulflow { get; set; }
	[JsonPropertyName("favorite_arrow")] public string? FavoriteArrow { get; set; }
}

public class RawDungeonsResponse {
	[JsonPropertyName("dungeon_types")]
	public DungeonTypes? DungeonTypes { get; set; }
	
	[JsonPropertyName("player_classes")]
	public DungeonPlayerClasses? PlayerClasses { get; set; }
	
	public long Secrets { get; set; }
}

public class DungeonTypes {
	public DungeonData Catacombs { get; set; } = new();
	[JsonPropertyName("master_catacombs")]
	public DungeonData MasterCatacombs { get; set; } = new();
}

public class DungeonData {
	public double Experience { get; set; }
}

public class DungeonPlayerClasses {
	public DungeonClassData Healer { get; set; } = new();
	public DungeonClassData Mage { get; set; } = new();
	public DungeonClassData Berserk { get; set; } = new();
	public DungeonClassData Archer { get; set; } = new();
	public DungeonClassData Tank { get; set; } = new();
}

public class DungeonClassData {
	public double Experience { get; set; }
}
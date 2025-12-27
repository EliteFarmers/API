using System.Text.Json.Nodes;
using System.Text.Json;
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

	public RawBestiaryResponse? Bestiary { get; set; }

	public RawLeveling? Leveling { get; set; }

	public RawItemData? ItemData { get; set; }

	public MemberCurrenciesResponse? Currencies { get; set; }

	public RawDungeonsResponse? Dungeons { get; set; }

	[JsonPropertyName("inventory")] public MemberInventoriesResponse? Inventories { get; set; }

	[JsonPropertyName("garden_player_data")]
	public GardenPlayerDataResponse? Garden { get; set; }

	[JsonPropertyName("slayer")] public RawSlayerData? Slayer { get; set; }

	[JsonPropertyName("mining_core")] public RawMiningCoreResponse? MiningCore { get; set; }

	[JsonPropertyName("nether_island_player_data")]
	public NetherIslandPlayerDataResponse? NetherIslandPlayerData { get; set; }

	[JsonPropertyName("trophy_fish")] public TrophyFishStats? TrophyFish { get; set; }

	[JsonPropertyName("abiphone")] public MemberAbiphoneResponse? Abiphone { get; set; }

	[JsonPropertyName("rift")] public MemberRiftResponse? Rift { get; set; }

	[JsonPropertyName("objectives")] public MemberObjectivesResponse? Objectives { get; set; }
}

public class MemberObjectivesResponse
{
	[JsonPropertyName("tutorial")]
	public List<string> Tutorial { get; set; } = [];
}

public class GardenPlayerDataResponse
{
	public int Copper { get; set; }

	[JsonPropertyName("larva_consumed")] public int LarvaConsumed { get; set; }
}

public class RawBestiaryResponse
{
	[JsonPropertyName("kills")]
	[JsonConverter(typeof(BestiaryKillsConverter))]
	public RawBestiaryKills Kills { get; set; } = new();

	[JsonPropertyName("deaths")] public Dictionary<string, int> Deaths { get; set; } = new();
	[JsonPropertyName("milestone")] public RawBestiaryMilestone? Milestone { get; set; }

	// Flags observed in samples
	[JsonPropertyName("migrated_stats")] public bool? MigratedStats { get; set; }
	public bool? Migration { get; set; }
}

public sealed class RawBestiaryKills
{
	// Numeric mob kill counts (e.g., pest_beetle_1: 42)
	public Dictionary<string, int> MobKills { get; set; } = new(StringComparer.OrdinalIgnoreCase);

	public string? LastKilledMob { get; set; }

	public Dictionary<string, string>? AdditionalStringFields { get; set; }
}

public sealed class BestiaryKillsConverter : JsonConverter<RawBestiaryKills>
{
	public override RawBestiaryKills? Read(ref Utf8JsonReader reader, Type typeToConvert,
		JsonSerializerOptions options) {
		if (reader.TokenType == JsonTokenType.Null) return null;
		if (reader.TokenType != JsonTokenType.StartObject)
			throw new JsonException("Expected StartObject for bestiary.kills");

		var result = new RawBestiaryKills();

		while (reader.Read()) {
			if (reader.TokenType == JsonTokenType.EndObject)
				break;

			if (reader.TokenType != JsonTokenType.PropertyName)
				throw new JsonException("Expected PropertyName within bestiary.kills");

			var name = reader.GetString() ?? string.Empty;

			if (!reader.Read())
				throw new JsonException("Unexpected end when reading bestiary.kills value");

			// Known string-valued metadata keys
			if (string.Equals(name, "last_killed_mob", StringComparison.OrdinalIgnoreCase)) {
				result.LastKilledMob = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
				continue;
			}
			// Note: last_killed_mob_island belongs to slayer quest data, not bestiary

			// Numeric mob kill counts
			if (reader.TokenType == JsonTokenType.Number) {
				if (reader.TryGetInt32(out var count)) {
					result.MobKills[name] = count;
				}
				else {
					// If it's a number but not an int32, try to parse as double and floor it safely
					if (reader.TryGetInt64(out var l))
						result.MobKills[name] = (int)Math.Clamp(l, int.MinValue, int.MaxValue);
				}

				continue;
			}

			// Capture any other string values for forward-compat
			if (reader.TokenType == JsonTokenType.String) {
				result.AdditionalStringFields ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
				result.AdditionalStringFields[name] = reader.GetString() ?? string.Empty;
				continue;
			}

			// Skip unsupported types to remain resilient
			reader.Skip();
		}

		return result;
	}

	public override void Write(Utf8JsonWriter writer, RawBestiaryKills value, JsonSerializerOptions options) {
		writer.WriteStartObject();

		// Write numeric mob kill counts
		foreach (var kvp in value.MobKills) {
			writer.WriteNumber(kvp.Key, kvp.Value);
		}

		// Write known string metadata fields
		if (!string.IsNullOrEmpty(value.LastKilledMob))
			writer.WriteString("last_killed_mob", value.LastKilledMob);

		// Write any additional string fields
		if (value.AdditionalStringFields is not null) {
			foreach (var kvp in value.AdditionalStringFields) {
				// Avoid duplicating known fields if they exist in the additional map
				if (string.Equals(kvp.Key, "last_killed_mob", StringComparison.OrdinalIgnoreCase))
					continue;

				writer.WriteString(kvp.Key, kvp.Value);
			}
		}

		writer.WriteEndObject();
	}
}

public class RawBestiaryMilestone
{
	[JsonPropertyName("last_claimed_milestone")]
	public int LastClaimedMilestone { get; set; }
}

public class RawAccessoryBagStorage
{
	[JsonPropertyName("tuning")] public AccessoryBagTuningSlots? Tuning { get; set; }
	[JsonPropertyName("selected_power")] public string? SelectedPower { get; set; }

	[JsonPropertyName("highest_magical_power")]
	public int HighestMagicalPower { get; set; }

	[JsonPropertyName("bag_upgrades_purchased")]
	public int BagUpgradesPurchased { get; set; }

	[JsonPropertyName("unlocked_powers")] public List<string> UnlockedPowers { get; set; } = [];
}

public class AccessoryBagTuning
{
	public int Health { get; set; }
	public int Defense { get; set; }
	[JsonPropertyName("walk_speed")] public int Speed { get; set; }
	public int Strength { get; set; }
	[JsonPropertyName("critical_damage")] public int CriticalDamage { get; set; }
	[JsonPropertyName("critical_chance")] public int CriticalChance { get; set; }
	public int Intelligence { get; set; }
	[JsonPropertyName("attack_speed")] public int AttackSpeed { get; set; }
}

public class AccessoryBagTuningSlots
{
	[JsonPropertyName("slot_0")] public AccessoryBagTuning? Slot0 { get; set; }
	[JsonPropertyName("slot_1")] public AccessoryBagTuning? Slot1 { get; set; }
	[JsonPropertyName("slot_2")] public AccessoryBagTuning? Slot2 { get; set; }
	[JsonPropertyName("slot_3")] public AccessoryBagTuning? Slot3 { get; set; }
	[JsonPropertyName("slot_4")] public AccessoryBagTuning? Slot4 { get; set; }

	[JsonPropertyName("highest_unlocked_slot")]
	public int? HighestUnlockedSlot { get; set; }

	[JsonPropertyName("refund_1")] public bool? Refund1 { get; set; }
}

public class RawItemData
{
	public long Soulflow { get; set; }
	[JsonPropertyName("favorite_arrow")] public string? FavoriteArrow { get; set; }
}

public class RawDungeonsResponse
{
	[JsonPropertyName("dungeon_types")] public DungeonTypes? DungeonTypes { get; set; }

	[JsonPropertyName("player_classes")] public DungeonPlayerClasses? PlayerClasses { get; set; }

	public long Secrets { get; set; }
}

public class DungeonTypes
{
	public DungeonData Catacombs { get; set; } = new();
	[JsonPropertyName("master_catacombs")] public DungeonData MasterCatacombs { get; set; } = new();
}

public class DungeonData
{
	public double Experience { get; set; }

	[JsonPropertyName("highest_tier_completed")]
	public int HighestTierCompleted { get; set; }

	[JsonPropertyName("tier_completions")] public Dictionary<string, double> TierCompletions { get; set; } = new();
	[JsonPropertyName("times_played")] public Dictionary<string, double> TimesPlayed { get; set; } = new();
	[JsonPropertyName("best_score")] public Dictionary<string, double> BestScore { get; set; } = new();
	[JsonPropertyName("mobs_killed")] public Dictionary<string, double> MobsKilled { get; set; } = new();
	[JsonPropertyName("most_mobs_killed")] public Dictionary<string, double> MostMobsKilled { get; set; } = new();
	[JsonPropertyName("watcher_kills")] public Dictionary<string, double> WatcherKills { get; set; } = new();
	[JsonPropertyName("most_healing")] public Dictionary<string, double> MostHealing { get; set; } = new();
	[JsonPropertyName("fastest_time")] public Dictionary<string, double> FastestTime { get; set; } = new();
	[JsonPropertyName("fastest_time_s")] public Dictionary<string, double> FastestTimeS { get; set; } = new();

	[JsonPropertyName("fastest_time_s_plus")]
	public Dictionary<string, double> FastestTimeSPlus { get; set; } = new();

	[JsonPropertyName("most_damage_tank")] public Dictionary<string, double> MostDamageTank { get; set; } = new();

	[JsonPropertyName("most_damage_mage")] public Dictionary<string, double> MostDamageMage { get; set; } = new();

	[JsonPropertyName("most_damage_healer")]
	public Dictionary<string, double> MostDamageHealer { get; set; } = new();

	[JsonPropertyName("most_damage_archer")]
	public Dictionary<string, double> MostDamageArcher { get; set; } = new();

	[JsonPropertyName("milestone_completions")]
	public Dictionary<string, double> MilestoneCompletions { get; set; } = new();
}

public class DungeonPlayerClasses
{
	public DungeonClassData Healer { get; set; } = new();
	public DungeonClassData Mage { get; set; } = new();
	public DungeonClassData Berserk { get; set; } = new();
	public DungeonClassData Archer { get; set; } = new();
	public DungeonClassData Tank { get; set; } = new();
}

public class DungeonClassData
{
	public double Experience { get; set; }
}
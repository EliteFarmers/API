using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Pet = EliteAPI.Models.Entities.Hypixel.Pet;

namespace EliteAPI.Models.DTOs.Incoming;

public class RawProfilesResponse
{
    public bool Success { get; set; }
    public RawProfileData[]? Profiles { get; set; }
}

public class RawProfileData
{
    public required Dictionary<string, RawMemberData> Members { get; set; }
    public RawBanking? Banking { get; set; }
    public bool Selected { get; set; } = false;

    [JsonPropertyName("cute_name")]
    public required string CuteName { get; set; }

    [JsonPropertyName("profile_id")]
    public required string ProfileId { get; set; }

    [JsonPropertyName("game_mode")]
    public string? GameMode { get; set; }

    [JsonPropertyName("community_upgrades")]
    public RawCommunityUpgrades? CommunityUpgrades { get; set; }

    [JsonPropertyName("last_save")]
    public long LastSave { get; set; }
}

public class RawMemberData
{
    public Pet[]? Pets { get; set; }

    public JsonObject? Dungeons { get; set; }

    [JsonPropertyName("slayer_bosses")]
    public JsonObject? SlayerBosses { get; set; }

    [JsonPropertyName("nether_island_player_data")]
    public RawNetherPlayerData? NetherIslandPlayerData { get; set; }

    [JsonPropertyName("jacob2")]
    public RawJacobData? Jacob { get; set; }

    public Dictionary<string, double> Stats { get; set; } = new();

    [JsonPropertyName("death_count")]
    public long? DeathCount { get; set; }

    [JsonPropertyName("harp_quest")]
    public JsonObject? HarpQuest { get; set; }

    public JsonObject? Experimentation { get; set; }

    [JsonPropertyName("first_join_hub")]
    public long? FirstJoinHub { get; set; }

    [JsonPropertyName("personal_bank_upgrade")]
    public int? PersonalBankUpgrade { get; set; }

    [JsonPropertyName("fairy_souls")]
    public int FairySouls { get; set; }

    [JsonPropertyName("fairy_exchanges")]
    public int? FairyExchanges { get; set; }

    [JsonPropertyName("fairy_souls_collected")]
    public int? FairySoulsCollected { get; set; }

    public JsonObject? Bestiary { get; set; }

    public string[]? Tutorial { get; set; }

    public Dictionary<string, int> Perks { get; set; } = new();

    [JsonPropertyName("visited_zones")]
    public string[]? VisitedZones { get; set; }

    public int? Soulflow { get; set; }
    public JsonObject? Quests { get; set; }

    [JsonPropertyName("coin_purse")]
    public double? CoinPurse { get; set; }

    public JsonObject? Autopet { get; set; }

    [JsonPropertyName("inv_armor")]
    public JsonObject? Armor { get; set; }

    [JsonPropertyName("accessory_bag_storage")]
    public JsonObject? AccessoryBag { get; set; }

    public RawLeveling? Leveling { get; set; }

    [JsonPropertyName("crafted_generators")]
    public string[]? CraftedGenerators { get; set; }

    [JsonPropertyName("visited_modes")]
    public string[]? VisitedModes { get; set; }

    [JsonPropertyName("achievement_spawned_island_types")]
    public string[]? AchievementSpawnedIslandTypes { get; set; }

    [JsonPropertyName("trapper_quest")]
    public JsonObject? TrapperQuest { get; set; }

    [JsonPropertyName("mining_core")]
    public JsonObject? MiningCore { get; set; }

    [JsonPropertyName("trophy_fish")]
    public JsonObject? TrophyFish { get; set; }

    public JsonObject? Forge { get; set; }

    public JsonObject? Objectives { get; set; }

    [JsonPropertyName("last_death")]
    public int LastDeath { get; set; }

    [JsonPropertyName("FirstJoin")]
    public long FirstJoin { get; set; }

    [JsonPropertyName("slayer_quest")]
    public JsonObject? SlayerQuest { get; set; }

    [JsonPropertyName("experience_skill_runecrafting")]
    public double? ExperienceSkillRunecrafting { get; set; }
    [JsonPropertyName("experience_skill_mining")]
    public double? ExperienceSkillMining { get; set; }
    [JsonPropertyName("experience_skill_alchemy")]
    public double? ExperienceSkillAlchemy { get; set; }
    [JsonPropertyName("experience_skill_combat")]
    public double? ExperienceSkillCombat { get; set; }
    [JsonPropertyName("experience_skill_farming")]
    public double? ExperienceSkillFarming { get; set; }
    [JsonPropertyName("experience_skill_taming")]
    public double? ExperienceSkillTaming { get; set; }
    [JsonPropertyName("experience_skill_social2")]
    public double? ExperienceSkillSocial { get; set; }
    [JsonPropertyName("experience_skill_enchanting")]
    public double? ExperienceSkillEnchanting { get; set; }
    [JsonPropertyName("experience_skill_fishing")]
    public double? ExperienceSkillFishing { get; set; }
    [JsonPropertyName("experience_skill_foraging")]
    public double? ExperienceSkillForaging { get; set; }
    [JsonPropertyName("experience_skill_carpentry")]
    public double? ExperienceSkillCarpentry { get; set; }

    [JsonPropertyName("equippment_contents")]
    public JsonObject? EquipmentContents { get; set; }

    [JsonPropertyName("unlocked_coll_tiers")]
    public string[]? UnlockedCollTiers { get; set; }

    [JsonPropertyName("backpack_contents")]
    public JsonObject? BackpackContents { get; set; }

    public JsonObject? Quiver { get; set; }

    [JsonPropertyName("sacks_counts")]
    public Dictionary<string, long> SackContents { get; set; } = new();

    [JsonPropertyName("essence_undead")]
    public long? EssenceUndead { get; set; }
    [JsonPropertyName("essence_dragon")]
    public long? EssenceDragon { get; set; }
    [JsonPropertyName("essence_gold")]
    public long? EssenceGold { get; set; }
    [JsonPropertyName("essence_diamond")]
    public long? EssenceDiamond { get; set; }
    [JsonPropertyName("essence_crimson")]
    public long? EssenceCrimson { get; set; }
    [JsonPropertyName("essence_spider")]
    public long? EssenceSpider { get; set; }
    [JsonPropertyName("essence_wither")]
    public long? EssenceWither { get; set; }
    [JsonPropertyName("essence_ice")]
    public long? EssenceIce { get; set; }

    public JsonDocument? Collection { get; set; }

    [JsonPropertyName("talisman_bag")]
    public JsonObject? TalismanBag { get; set; }

    [JsonPropertyName("backpack_icons")]
    public JsonObject? BackpackIcons { get; set; }

    [JsonPropertyName("fishing_bag")]
    public JsonObject? FishingBag { get; set; }

    [JsonPropertyName("wardrobe_equipped_slot")]
    public int? WardrobeEquippedSlot { get; set; }

    [JsonPropertyName("ender_chest_contents")]
    public JsonObject? EnderChestContents { get; set; }

    [JsonPropertyName("wardrobe_contents")]
    public JsonObject? WardrobeContents { get; set; }

    [JsonPropertyName("potion_bag")]
    public JsonObject? PotionBag { get; set; }

    [JsonPropertyName("inv_contents")]
    public JsonObject? InventoryContents { get; set; }

    [JsonPropertyName("candy_inventory_contents")]
    public JsonObject? CandyInventoryContents { get; set; }
}

public class RawLeveling
{
    public int? Experience { get; set; }
}

public class RawNetherPlayerData
{
    public JsonObject? Dojo { get; set; }
    public Abiphone? Abiphone { get; set; }
    public JsonObject? Matriarch { get; set; }

    [JsonPropertyName("kuudra_completed_tiers")]
    public JsonObject? KuudraCompletedTiers { get; set; }

    [JsonPropertyName("mages_reputation")]
    public float? MageReputation { get; set; }

    [JsonPropertyName("barbarians_reputation")]
    public float? BarbarianReputation { get; set; }

    [JsonPropertyName("selected_faction")]
    public string? SelectedFaction { get; set; }

    [JsonPropertyName("last_minibosses_killed")]
    public string[]? LastMinibossesKilled { get; set; }

    [JsonPropertyName("kuudra_party_finder")]
    public JsonObject? KuudraPartyFinder { get; set; }
}

public class Abiphone
{
    [JsonPropertyName("contact_data")]
    public JsonObject? ContactData { get; set; }

    public JsonObject? Games { get; set; }

    [JsonPropertyName("active_contacts")]
    public string[]? ActiveContacts { get; set; }

    [JsonPropertyName("operator_chip")]
    public JsonObject? OperatorChip { get; set; }

    [JsonPropertyName("trio_contact_addons")]
    public int TrioContactAddons { get; set; }
}
public class RawJacobData
{
    public RawJacobPerks? Perks { get; set; }
    public bool Talked { get; set; }
    public Dictionary<string, RawJacobContest> Contests { get; set; } = new();

    [JsonPropertyName("medals_inv")]
    public RawMedalsInventory? MedalsInventory { get; set; }

    [JsonPropertyName("unique_golds2")]
    public string[]? UniqueGolds { get; set; }
}

public class RawMedalsInventory
{
    public int Bronze { get; set; }
    public int Silver { get; set; }
    public int Gold { get; set; }
}

public class RawJacobPerks
{
    [JsonPropertyName("farming_level_cap")]
    public int? FarmingLevelCap { get; set; }

    [JsonPropertyName("double_drops")]
    public int? DoubleDrops { get; set; }
}

public class RawJacobContest
{
    public int Collected { get; set; }

    [JsonPropertyName("claimed_rewards")]
    public bool? ClaimedRewards { get; set; }

    [JsonPropertyName("claimed_position")]
    public int? Position { get; set; }

    [JsonPropertyName("claimed_participants")]
    public int? Participants { get; set; }

    [JsonPropertyName("claimed_medal")]
    public string? Medal { get; set; }
}

public class RawCoopInvitation
{
    public long Timestamp { get; set; }
    public bool Confirmed { get; set; }

    [JsonPropertyName("invited_by")]
    public required string InvitedBy { get; set; }

    [JsonPropertyName("confirmed_timestamp")]
    public long ConfirmedTimestamp { get; set; }
}

public class RawPetData
{
    public string? Uuid { get; set; }
    public required string Type { get; set; }
    public double Exp { get; set; }
    public bool Active { get; set; } = false;
    public required string Tier { get; set; }
    public string? HeldItem { get; set; }
    public int CandyUsed { get; set; }
    public string? Skin { get; set; }
}

public class RawCommunityUpgrades
{
    [JsonPropertyName("currently_upgrading")]
    public RawCurrentlyUpgrading? CurrentlyUpgrading { get; set; }

    [JsonPropertyName("upgrade_states")]
    public RawUpgradeStates[] UpgradeStates { get; set; } = Array.Empty<RawUpgradeStates>();
}

public class RawCurrentlyUpgrading
{
    public required string Upgrade { get; set; }

    [JsonPropertyName("new_tier")]
    public int NewTier { get; set; }

    [JsonPropertyName("start_ms")]
    public long StartMs { get; set; }

    [JsonPropertyName("who_started")]
    public required string WhoStarted { get; set; }
}

public class RawUpgradeStates
{
    public required string Upgrade { get; set; }
    public int Tier { get; set; }

    [JsonPropertyName("started_ms")]
    public long StartedMs { get; set; }

    [JsonPropertyName("started_by")]
    public required string StartedBy { get; set; }

    [JsonPropertyName("claimed_ms")]
    public long ClaimedMs { get; set; }

    [JsonPropertyName("claimed_by")]
    public required string ClaimedBy { get; set; }

    [JsonPropertyName("fasttracked")]
    public bool FastTracked { get; set; }
}

public class RawBanking
{
    public double Balance { get; set; }
    public RawTransaction[] Transactions { get; set; } = Array.Empty<RawTransaction>();
}

public class RawTransaction
{
    public float Amount { get; set; }
    public long Timestamp { get; set; }
    public required string Action { get; set; }

    [JsonPropertyName("initiator_name")]
    public required string Initiator { get; set; }
}

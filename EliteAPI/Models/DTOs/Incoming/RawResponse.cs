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
    [JsonPropertyName("player_data")]
    public RawMemberPlayerData? PlayerData { get; set; }
    
    [JsonPropertyName("pets_data")]
    public RawMemberPetsData? PetsData { get; set; }
    
    public RawMemberProfileData? Profile { get; set; }
    
    [JsonPropertyName("jacobs_contest")]
    public RawJacobData? Jacob { get; set; }
    
    public JsonDocument? Collection { get; set; }
    
    [JsonPropertyName("accessory_bag_storage")]
    public JsonObject? AccessoryBagSettings { get; set; }
    public JsonObject? Bestiary { get; set; }
    
    public RawLeveling? Leveling { get; set; }
    
    [JsonPropertyName("deletion_notice")]
    public JsonObject? DeletionNotice { get; set; }

    public RawMemberCurrencies? Currencies { get; set; }
    
    [JsonPropertyName("inventory")]
    public RawMemberInventories? Inventories { get; set; }

    //
    // public Dictionary<string, double> Stats { get; set; } = new();
    //
    // [JsonPropertyName("death_count")]
    // public long? DeathCount { get; set; }
    //
    // [JsonPropertyName("harp_quest")]
    // public JsonObject? HarpQuest { get; set; }
    //
    // public JsonObject? Experimentation { get; set; }
    //
    // [JsonPropertyName("first_join_hub")]
    // public long? FirstJoinHub { get; set; }
    //
    // [JsonPropertyName("personal_bank_upgrade")]
    // public int? PersonalBankUpgrade { get; set; }
    //
    // [JsonPropertyName("fairy_souls")]
    // public int FairySouls { get; set; }
    //
    // [JsonPropertyName("fairy_exchanges")]
    // public int? FairyExchanges { get; set; }
    //
    // [JsonPropertyName("fairy_souls_collected")]
    // public int? FairySoulsCollected { get; set; }
    //
    // public JsonObject? Bestiary { get; set; }
    //
    // public string[]? Tutorial { get; set; }
    //
    //
    // [JsonPropertyName("visited_zones")]
    // public string[]? VisitedZones { get; set; }
    //
    // public int? Soulflow { get; set; }
    // public JsonObject? Quests { get; set; }
    //
}

public class RawMemberPlayerData {
    public Dictionary<string, int> Perks { get; set; } = new();
    
    [JsonPropertyName("crafted_generators")]
    public string[]? CraftedGenerators { get; set; }
    
    [JsonPropertyName("unlocked_coll_tiers")]
    public string[]? UnlockedCollTiers { get; set; }
    
    [JsonPropertyName("temp_stat_buffs")]
    public List<TempStatBuff> TempStatBuffs { get; set; } = new();

    public RawPlayerExperience? Experience { get; set; } = new();
}

public class RawMemberPetsData {
    public Pet[]? Pets { get; set; }
}

public class RawMemberCurrencies {
    [JsonPropertyName("coin_purse")]
    public double? CoinPurse { get; set; }
}

public class RawPlayerExperience {
    [JsonPropertyName("SKILL_RUNECRAFTING")]
    public double? SkillRunecrafting { get; set; }
    [JsonPropertyName("SKILL_MINING")]
    public double? SkillMining { get; set; }
    [JsonPropertyName("SKILL_ALCHEMY")]
    public double? SkillAlchemy { get; set; }
    [JsonPropertyName("SKILL_COMBAT")]
    public double? SkillCombat { get; set; }
    [JsonPropertyName("SKILL_FARMING")]
    public double? SkillFarming { get; set; }
    [JsonPropertyName("SKILL_TAMING")]
    public double? SkillTaming { get; set; }
    [JsonPropertyName("SKILL_SOCIAL")]
    public double? SkillSocial { get; set; }
    [JsonPropertyName("SKILL_ENCHANTING")]
    public double? SkillEnchanting { get; set; }
    [JsonPropertyName("SKILL_FISHING")]
    public double? SkillFishing { get; set; }
    [JsonPropertyName("SKILL_FORAGING")]
    public double? SkillForaging { get; set; }
    [JsonPropertyName("SKILL_CARPENTRY")]
    public double? SkillCarpentry { get; set; }
}

public class RawMemberProfileData {
    [JsonPropertyName("coop_invitation")]
    public RawCoopInvitation? CoopInvitation { get; set; }
}

public class RawMemberInventories {
    [JsonPropertyName("wardrobe_equipped_slot")]
    public int? WardrobeEquippedSlot { get; set; }
    
    [JsonPropertyName("wardrobe_contents")]
    public RawInventoryData? WardrobeContents { get; set; }
    
    [JsonPropertyName("inv_armor")]
    public RawInventoryData? Armor { get; set; }
    
    [JsonPropertyName("equipment_contents")]
    public RawInventoryData? EquipmentContents { get; set; }
    
    [JsonPropertyName("bag_contents")]
    public RawMemberBagContents? BagContents { get; set; }
    
    [JsonPropertyName("inv_contents")]
    public RawInventoryData? InventoryContents { get; set; }

    [JsonPropertyName("backpack_icons")]
    public RawInventoryData? BackpackIcons { get; set; }
    
    [JsonPropertyName("personal_vault_contents")]
    public RawInventoryData? PersonalVaultContents { get; set; }
    
    [JsonPropertyName("sacks_counts")]
    public Dictionary<string, long> SackContents { get; set; } = new();
    
    [JsonPropertyName("backpack_contents")]
    public Dictionary<int, RawInventoryData>? BackpackContents { get; set; }

    [JsonPropertyName("ender_chest_contents")]
    public RawInventoryData? EnderChestContents { get; set; }
}

public class RawMemberBagContents {
    [JsonPropertyName("fishing_bag")]
    public RawInventoryData? FishingBag { get; set; }
    
    [JsonPropertyName("talisman_bag")]
    public RawInventoryData? TalismanBag { get; set; }
    
    [JsonPropertyName("potion_bag")]
    public RawInventoryData? PotionBag { get; set; }

}

public class RawInventoryData
{
    public int Type { get; set; }
    public string Data { get; set; } = string.Empty;
}

public class RawLeveling
{
    public int? Experience { get; set; }
}

public class RawJacobData
{
    public RawJacobPerks? Perks { get; set; }
    public bool Talked { get; set; }
    public Dictionary<string, RawJacobContest> Contests { get; set; } = new();

    [JsonPropertyName("medals_inv")]
    public RawMedalsInventory? MedalsInventory { get; set; }

    [JsonPropertyName("unique_brackets")]
    public RawJacobUniqueBrackets? UniqueBrackets { get; set; }

    [JsonPropertyName("personal_bests")]
    public Dictionary<string, long> PersonalBests { get; set; } = new();
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

public class RawJacobUniqueBrackets {
    public List<string> Bronze { get; set; } = new();
    public List<string> Silver { get; set; } = new();
    public List<string> Gold { get; set; } = new();
    public List<string> Platinum { get; set; } = new();
    public List<string> Diamond { get; set; } = new();
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

public class TempStatBuff {
    public int Stat { get; set; }
    public string? Key { get; set; }
    public int Amount { get; set; }
    [JsonPropertyName("expire_at")]
    public long ExpireAt { get; set; }
}
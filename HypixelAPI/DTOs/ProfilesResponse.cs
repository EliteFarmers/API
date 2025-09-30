using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.Converters;

namespace EliteFarmers.HypixelAPI.DTOs;

public class ProfilesResponse {
    public bool Success { get; set; }
    public ProfileResponse[]? Profiles { get; set; }
}

public class RawMemberEvents {
    public EasterEventDataResponse? Easter { get; set; }
}

public class EasterEventDataResponse {
    public long Chocolate { get; set; }
    [JsonPropertyName("total_chocolate")]
    public long TotalChocolate { get; set; }
    [JsonPropertyName("chocolate_since_prestige")]
    public long ChocolateSincePrestige { get; set; }
    
    [JsonPropertyName("last_viewed_chocolate_factory")]
    public long LastViewedChocolateFactory { get; set; }
    [JsonPropertyName("chocolate_level")]
    public int Prestige { get; set; }
    
    public RawMemberEasterEventShop Shop { get; set; } = new();
    
    [JsonPropertyName("rabbit_barn_capacity_level")]
    public int RabbitBarnCapacityLevel { get; set; }
    [JsonPropertyName("chocolate_multiplier_upgrades")]
    public int ChocolateLevel { get; set; }
    
    [JsonConverter(typeof(RabbitDictionaryConverter))]
    public Dictionary<string, int> Rabbits { get; set; } = new();
    
    [JsonPropertyName("refined_dark_cacao_truffles")]
    public int RefinedDarkCacaoTrufflesConsumed { get; set; }
}

public class RawMemberEasterEventShop {
    [JsonPropertyName("chocolate_spent")]
    public long ChocolateSpent { get; set; }
    
    [JsonPropertyName("cocoa_fortune_upgrades")]
    public int CocoaFortuneUpgrades { get; set; }
}

public class RawMemberPlayerData {
    public Dictionary<string, int> Perks { get; set; } = new();
    
    [JsonPropertyName("crafted_generators")]
    public string[]? CraftedGenerators { get; set; }
    
    [JsonPropertyName("unlocked_coll_tiers")]
    public string[]? UnlockedCollTiers { get; set; }
    
    [JsonPropertyName("temp_stat_buffs")]
    public List<TempStatBuffResponse> TempStatBuffs { get; set; } = new();

    public MemberExperienceResponse? Experience { get; set; } = new();
}

public class MemberPetsResponse {
    public PetResponse[]? Pets { get; set; }
    [JsonPropertyName("pet_care")]
    public PetCareResponse? PetCare { get; set; }
}

public class PetResponse {
    public string? Uuid { get; set; }
    public string? Type { get; set; }
    public double Exp { get; set; } = 0;
    public bool Active { get; set; } = false;
    public string? Tier { get; set; }
    public string? HeldItem { get; set; }
    public short CandyUsed { get; set; } = 0;
    public string? Skin { get; set; }
}

public class PetCareResponse {
    [JsonPropertyName("pet_types_sacrificed")]
    public List<string> PetTypesSacrificed { get; set; } = [];
}

public class MemberCurrenciesResponse {
    [JsonPropertyName("coin_purse")]
    public double? CoinPurse { get; set; }
}

public class MemberExperienceResponse {
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

public class MemberProfileDataResponse {
    [JsonPropertyName("coop_invitation")]
    public RawCoopInvitation? CoopInvitation { get; set; }
    
    [JsonPropertyName("deletion_notice")]
    public JsonObject? DeletionNotice { get; set; }
}

public class MemberInventoriesResponse {
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
    
    [JsonPropertyName("personal_bests")]
    public bool PersonalBests { get; set; } = false;
}

public class RawJacobUniqueBrackets {
    public List<string> Bronze { get; set; } = [];
    public List<string> Silver { get; set; } = [];
    public List<string> Gold { get; set; } = [];
    public List<string> Platinum { get; set; } = [];
    public List<string> Diamond { get; set; } = [];
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

public class ProfileCommunityUpgrades
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

public class ProfileBankingResponse
{
    public double Balance { get; set; }
    public RawTransaction[] Transactions { get; set; } = [];
}

public class RawTransaction
{
    public float Amount { get; set; }
    public long Timestamp { get; set; }
    public required string Action { get; set; }

    [JsonPropertyName("initiator_name")]
    public required string Initiator { get; set; }
}

public class TempStatBuffResponse {
    public int Stat { get; set; }
    public string? Key { get; set; }
    public int Amount { get; set; }
    [JsonPropertyName("expire_at")]
    public long ExpireAt { get; set; }
}
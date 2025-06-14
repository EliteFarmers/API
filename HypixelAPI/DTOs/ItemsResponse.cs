using System.Text.Json;
using System.Text.Json.Serialization;

namespace HypixelAPI.DTOs;

public class ItemsResponse {
	public bool Success { get; set; }
	public long LastUpdated { get; set; }
	
	[JsonPropertyName("items")]
	public List<ItemResponse> Items { get; set; } = [];
}

public class ItemResponse
{
	[JsonPropertyName("id")]
	public string? Id { get; set; }
	[JsonPropertyName("material")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Material { get; set; }
    /// <summary>
    /// Color as R,G,B
    /// </summary>
    [JsonPropertyName("color")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Color { get; set; }
    [JsonPropertyName("durability")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Durability { get; set; }
    [JsonPropertyName("skin")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ItemSkin? Skin { get; set; }
    [JsonPropertyName("name")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; set; }
    [JsonPropertyName("category")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Category { get; set; }
    
    [JsonPropertyName("tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Tier { get; set; }
    
    [JsonPropertyName("unstackable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Unstackable { get; set; }
    
    [JsonPropertyName("glowing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Glowing { get; set; }
    
    [JsonPropertyName("npc_sell_price")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double NpcSellPrice { get; set; }
    
    [JsonPropertyName("can_auction")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CanAuction { get; set; }
    
    [JsonPropertyName("can_trade")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CanTrade { get; set; }
    
    // [JsonPropertyName("has_uuid")]
    // public bool HasUuid { get; set; }
    
    [JsonPropertyName("can_place")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool CanPlace { get; set; }
    
    [JsonPropertyName("gemstone_slots")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ItemGemstoneSlot>? GemstoneSlots { get; set; }
    
    [JsonPropertyName("requirements")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<ItemRequirement>? Requirements { get; set; }
    
    [JsonPropertyName("museum")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Museum { get; set; } = false;
    
    [JsonPropertyName("museum_data")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ItemMuseumData? MuseumData { get; set; }
    
    [JsonPropertyName("stats")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public Dictionary<string, double>? Stats { get; set; }
    
    [JsonPropertyName("generator_tier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int GeneratorTier { get; set; }
    
    [JsonPropertyName("dungeon_item_conversion_cost")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public DungeonItemConversionCost? DungeonItemConversionCost { get; set; }
    
    [JsonPropertyName("upgrade_costs")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public UpgradeCosts[][]? UpgradeCosts { get; set; }
    
    [JsonPropertyName("catacombs_requirements")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<CatacombsRequirements>? CatacombsRequirements { get; set; }
    
    [JsonPropertyName("hide_from_viewrecipe_command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool HideFromViewRecipeCommand { get; set; } = false;
    
    [JsonPropertyName("salvagable_from_recipe")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool SalvagableFromRecipe { get; set; } = false;
    
    [JsonPropertyName("item_specific")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public JsonDocument? ItemSpecific { get; set; }
    
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public class ItemSkin
{
	[JsonPropertyName("value")]
    public string Value { get; set; }
    
    [JsonPropertyName("signature")]
    public string Signature { get; set; }
}

public class ItemGemstoneSlot {
	[JsonPropertyName("slot_type")]
	public string SlotType { get; set; }
	[JsonPropertyName("costs")]
	public List<ItemGemstoneSlotCosts> Costs { get; set; } = [];
}

public class ItemGemstoneSlotCosts {
	[JsonPropertyName("type")]
	public required string Type { get; set; }
	[JsonPropertyName("item_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? ItemId { get; set; }
	[JsonPropertyName("coins"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Coins { get; set; }
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; } = new();
}

public class ItemRequirement {
	[JsonPropertyName("type")]
	public required string Type { get; set; }
	[JsonPropertyName("skill"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Skill { get; set; }
	[JsonPropertyName("level"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Level { get; set; }
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; } = new();
}

public class ItemMuseumData {
	[JsonPropertyName("donation_xp")]
	public int DonationXp { get; set; }
	[JsonPropertyName("parent"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, string> Parent { get; set; } = new();
	[JsonPropertyName("type")]
	public string? Type { get; set; }
	[JsonPropertyName("armor_set_donation_xp"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, int>? ArmorSetDonationXp { get; set; }
	[JsonPropertyName("game_stage"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? GameStage { get; set; }
	
	[JsonExtensionData]
	public Dictionary<string, JsonElement>? ExtensionData { get; set; } = new();
}

public class DungeonItemConversionCost
{
    [JsonPropertyName("essence_type")]
    public string? EssenceType { get; set; }
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}

public class UpgradeCosts
{
	[JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("essence_type")]
    public string? EssenceType { get; set; }
    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}

public class CatacombsRequirements
{
	[JsonPropertyName("type")]
    public string? Type { get; set; }
    [JsonPropertyName("dungeon_type")]
    public string? DungeonType { get; set; }
    [JsonPropertyName("level")]
    public int Level { get; set; }
}
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public sealed class MemberRiftResponse
{
    [JsonPropertyName("access")]
    public MemberRiftAccessResponse? Access { get; set; }

    [JsonPropertyName("inventory")]
    public MemberRiftInventoryResponse? Inventory { get; set; }

    [JsonPropertyName("village_plaza")]
    public MemberRiftAreaResponse? VillagePlaza { get; set; }

    [JsonPropertyName("wither_cage")]
    public MemberRiftAreaResponse? WitherCage { get; set; }

    [JsonPropertyName("black_lagoon")]
    public MemberRiftAreaResponse? BlackLagoon { get; set; }

    [JsonPropertyName("dead_cats")]
    public MemberRiftAreaResponse? DeadCats { get; set; }

    [JsonPropertyName("wizard_tower")]
    public MemberRiftAreaResponse? WizardTower { get; set; }

    [JsonPropertyName("enigma")]
    public MemberRiftAreaResponse? Enigma { get; set; }

    [JsonPropertyName("gallery")]
    public MemberRiftAreaResponse? Gallery { get; set; }

    [JsonPropertyName("west_village")]
    public MemberRiftAreaResponse? WestVillage { get; set; }

    [JsonPropertyName("wyld_woods")]
    public MemberRiftAreaResponse? WyldWoods { get; set; }

    [JsonPropertyName("castle")]
    public MemberRiftAreaResponse? Castle { get; set; }

    [JsonPropertyName("dreadfarm")]
    public MemberRiftAreaResponse? Dreadfarm { get; set; }

    [JsonPropertyName("slayer_quest")]
    public MemberRiftSlayerQuestResponse? SlayerQuest { get; set; }

    [JsonPropertyName("lifetime_purchased_boundaries")]
    public IReadOnlyList<string>? LifetimePurchasedBoundaries { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}

public sealed class MemberRiftInventoryResponse
{
    [JsonPropertyName("inv_contents")]
    public RawInventoryData? InventoryContents { get; set; }

    [JsonPropertyName("inv_armor")]
    public RawInventoryData? Armor { get; set; }

    [JsonPropertyName("equipment_contents")]
    public RawInventoryData? EquipmentContents { get; set; }

    [JsonPropertyName("ender_chest_contents")]
    public RawInventoryData? EnderChestContents { get; set; }

    [JsonPropertyName("ender_chest_page_icons")]
    public IReadOnlyList<RawInventoryData?>? EnderChestPageIcons { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}

public sealed class MemberRiftAreaResponse
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}

public sealed class MemberRiftSlayerQuestResponse
{
    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}

public sealed class MemberRiftAccessResponse
{
    [JsonPropertyName("last_free")]
    public long? LastFree { get; set; }

    [JsonPropertyName("consumed_prism")]
    public bool? ConsumedPrism { get; set; }

    [JsonPropertyName("charge_track_timestamp")]
    public long? ChargeTrackTimestamp { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement> AdditionalData { get; set; } = [];
}

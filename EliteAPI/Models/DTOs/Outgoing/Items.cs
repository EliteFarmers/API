using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Outgoing;

public class ItemDto {
	/// <summary>
	/// Old Minecraft id of the item
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Minecraft stack count of the item
	/// </summary>
	public byte Count { get; set; }

	/// <summary>
	/// Skyblock ID of the item
	/// </summary>
	public string? SkyblockId { get; set; }

	/// <summary>
	/// Item UUID to uniquely identify a specific instance of this item
	/// </summary>
	public string? Uuid { get; set; }

	/// <summary>
	/// Item name, first line of the lore
	/// </summary>
	public string? Name { get; set; }

	/// <summary>
	/// List of item lore in order
	/// </summary>
	public List<string>? Lore { get; set; }

	/// <summary>
	/// Applied enchantments with their levels
	/// </summary>
	public Dictionary<string, int>? Enchantments { get; set; }

	/// <summary>
	/// ExtraAttributes not included elsewhere
	/// </summary>
	public Dictionary<string, string>? Attributes { get; set; }

	/// <summary>
	/// ExtraAtrributes.Attributes for attribute shards
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, string>? ItemAttributes { get; set; }

	/// <summary>
	/// Applied gems with gem rarity, null for an unlocked gem slot without a gem
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, string?>? Gems { get; set; }

	/// <summary>
	/// Pet info if item is a pet
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ItemPetInfoDto? PetInfo { get; set; }
}

public class ItemPetInfoDto {
	[JsonPropertyName("type")] public required string Type { get; set; }
	[JsonPropertyName("active")] public bool Active { get; set; }
	[JsonPropertyName("exp")] public decimal Exp { get; set; }
	public int Level { get; set; }
	[JsonPropertyName("tier")] public required string Tier { get; set; }

	[JsonPropertyName("candyUsed")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int CandyUsed { get; set; }

	[JsonPropertyName("heldItem")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? HeldItem { get; set; }

	[JsonExtensionData] public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
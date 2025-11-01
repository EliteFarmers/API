using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Outgoing;

public class ItemDto
{
	/// <summary>
	/// Old Minecraft id of the item
	/// </summary>
	public int Id { get; set; }

	/// <summary>
	/// Minecraft stack count of the item
	/// </summary>
	public byte Count { get; set; }

	/// <summary>
	/// Minecraft damage value of the item
	/// </summary>
	public short Damage { get; set; }

	/// <summary>
	/// Skyblock ID of the item
	/// </summary>
	public string? SkyblockId { get; set; }

	/// <summary>
	/// Item UUID to uniquely identify a specific instance of this item
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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
	public ItemAttributes? Attributes { get; set; }

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

	/// <summary>
	/// Image url for the item
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? ImageUrl { get; set; }

	/// <summary>
	/// Texture id for the item, used to look up the image in our image service
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? TextureId { get; set; }

	/// <summary>
	/// Slot identifier where the item was located, if applicable
	/// </summary>
	public string? Slot { get; set; } = string.Empty;
}

public class ItemPetInfoDto
{
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

public class ItemAttributes
{
	[JsonPropertyName("runes"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, int>? Runes { get; set; }
	
	[JsonPropertyName("effects"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ItemEffectAttribute>? Effects { get; set; }
	
	[JsonPropertyName("necromancer_souls"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<ItemSoulAttribute>? NecromancerSouls { get; set; }
	
	[JsonPropertyName("hook"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ItemRodPartAttribute? Hook { get; set; }
	
	[JsonPropertyName("line"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ItemRodPartAttribute? Line { get; set; }
	
	[JsonPropertyName("sinker"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ItemRodPartAttribute? Sinker { get; set; }
	
	[JsonPropertyName("ability_scroll"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public List<string>? AbilityScrolls { get; set; }
	
	[JsonPropertyName("inventory_data"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public Dictionary<string, ItemDto?>? Inventory { get; set; }
	
	[JsonExtensionData]
	public Dictionary<string, object> Extra { get; set; } = new Dictionary<string, object>();
	
	public static implicit operator ItemAttributes(Dictionary<string, string>? attributes) {
		var objDictionary = attributes?.ToDictionary(a => a.Key, (a) => (object)a.Value) ?? new Dictionary<string, object>();
		return new ItemAttributes() {
			Extra = objDictionary,
		};
	}
	
	public static implicit operator ItemAttributes(Dictionary<string, object>? attributes) {
		return new ItemAttributes() {
			Extra = attributes ?? new Dictionary<string, object>()
		};
	}
	
	public string? this[string index]
	{
		get => Extra.TryGetValue(index, out var value) ? value.ToString() : null;
		set => Extra[index] = value ?? string.Empty;
	}
	
	public bool Remove(string index) => Extra.Remove(index);

	public bool TryGetValue(string key, [NotNullWhen(true)] out string? value) {
		if (Extra.TryGetValue(key, out var objectValue)) {
			value = objectValue.ToString()!;
			return true;
		}

		value = null;
		return false;
	}
}

public class ItemEffectAttribute
{
	[JsonPropertyName("level"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int Level { get; set; }
	
	[JsonPropertyName("effect"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Effect {  get; set; }
	
	[JsonPropertyName("duration_ticks"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int DurationTicks { get; set; }
}

public class ItemSoulAttribute
{
	[JsonPropertyName("mob_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? MobId { get; set; }
	
	[JsonPropertyName("dropped_instance_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DroppedInstanceId { get; set; }
	
	[JsonPropertyName("dropped_mode_id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? DroppedModeId { get; set; }
}

public class ItemRodPartAttribute
{
	[JsonPropertyName("part"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Part { get; set; }
}
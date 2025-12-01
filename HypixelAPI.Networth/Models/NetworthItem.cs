using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace HypixelAPI.Networth.Models;

public class NetworthItem
{
	public int Id { get; set; }
	public int Count { get; set; }
	public short Damage { get; set; }
	public string? SkyblockId { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Uuid { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Name { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<string>? Lore { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, int>? Enchantments { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public NetworthItemAttributes? Attributes { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, string>? ItemAttributes { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, string?>? Gems { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<string>? UnlockedSlots { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public NetworthItemPetInfo? PetInfo { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? TextureId { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<NetworthItemGemstoneSlot>? GemstoneSlots { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<List<NetworthItemUpgradeCost>>? UpgradeCosts { get; set; }

	// Networth specific properties
	public double BasePrice { get; set; }
	public double Price { get; set; }
	public double SoulboundPortion { get; set; }
	public bool IsSoulbound { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<NetworthCalculation>? Calculation { get; set; }
}

public class NetworthItemGemstoneSlot
{
	public string SlotType { get; set; } = string.Empty;
	public List<NetworthItemCost>? Costs { get; set; }
}

public class NetworthItemUpgradeCost
{
	public string Type { get; set; } = string.Empty;
	public string? ItemId { get; set; }
	public int Amount { get; set; }
}

public class NetworthItemCost
{
	public string Type { get; set; } = string.Empty;
	public string? ItemId { get; set; }
	public int Amount { get; set; }
	public double? Coins { get; set; }
}

public class NetworthItemPetInfo
{
	public required string Type { get; set; }
	public bool Active { get; set; }
	public decimal Exp { get; set; }
	public int Level { get; set; }
	public required string Tier { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int CandyUsed { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? HeldItem { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public string? Skin { get; set; }
}

public class NetworthItemAttributes
{
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, int>? Runes { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<NetworthItemEffectAttribute>? Effects { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<NetworthItemSoulAttribute>? NecromancerSouls { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public NetworthItemRodPartAttribute? Hook { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public NetworthItemRodPartAttribute? Line { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public NetworthItemRodPartAttribute? Sinker { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public List<string>? AbilityScrolls { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, NetworthItem?>? Inventory { get; set; }
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public Dictionary<string, object> Extra { get; set; } = new Dictionary<string, object>();

	public string? this[string index] {
		get => Extra.TryGetValue(index, out var value) ? value.ToString() : null;
		set => Extra[index] = value ?? string.Empty;
	}

	public bool TryGetValue(string key, [NotNullWhen(true)] out string? value) {
		if (Extra.TryGetValue(key, out var objectValue)) {
			value = objectValue.ToString()!;
			return true;
		}

		value = null;
		return false;
	}
}

public class NetworthItemEffectAttribute
{
	public int Level { get; set; }
	public string? Effect { get; set; }
	public int DurationTicks { get; set; }
}

public class NetworthItemSoulAttribute
{
	public string? MobId { get; set; }
	public string? DroppedInstanceId { get; set; }
	public string? DroppedModeId { get; set; }
}

public class NetworthItemRodPartAttribute
{
	public string? Part { get; set; }
	public bool Donated { get; set; }
}
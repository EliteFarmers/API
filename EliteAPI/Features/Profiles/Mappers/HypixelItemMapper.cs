using System.Text.RegularExpressions;
using EliteAPI.Features.Images.Models;
using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.DTOs.Outgoing;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Profiles.Mappers;

[Mapper]
[UseStaticMapper(typeof(ImageMapper))]
public static partial class HypixelItemMapper
{
	public static HypixelInventoryDto ToDto(this HypixelInventory inventory) {
		var items = new Dictionary<string, ItemDto?>();
		if (inventory.EmptySlots is not null) {
			foreach (var i in inventory.EmptySlots) {
				items.TryAdd(i.ToString(), null);
			}
		}
		
		foreach (var i in inventory.Items) {
			var dto = i.ToDto();
			if (dto.Slot is null) continue;
			items.TryAdd(dto.Slot, dto);
		}
		
		return new HypixelInventoryDto() {
			Id = inventory.HypixelInventoryId,
			Name = inventory.Name,
			Items = items,
			Metadata = inventory.Metadata
		};
	}
	
	public static HypixelInventoryOverviewDto ToOverviewDto(this HypixelInventory inventory) {
		return new HypixelInventoryOverviewDto() {
			Id = inventory.HypixelInventoryId,
			Name = inventory.Name,
			Metadata = inventory.Metadata
		};
	}

	public static HypixelItem ToHypixelItem(this ItemDto dto) {
		var item = new HypixelItem {
			Uuid = Guid.TryParse(dto.Uuid, out var guid) ? guid : null,
			SkyblockId = dto.SkyblockId ?? "UNKNOWN",
			Id = (short)dto.Id,
			Damage = dto.Damage,
			Count = dto.Count,
			Name = dto.Name,
			Lore = dto.Lore != null ? string.Join("\n", dto.Lore) : null,
			Enchantments = dto.Enchantments,
			Gems = dto.Gems,
			Slot = dto.Slot,
			LastUpdated = DateTimeOffset.UtcNow
		};

		if (dto.Attributes is not null && dto.Attributes.Count > 0) {
			var modifer = dto.Attributes.GetValueOrDefault("modifier");
			if (modifer is not null) {
				dto.Attributes.Remove("modifier");
				item.Modifier = modifer;
			}

			var rarityUpgrades = dto.Attributes.GetValueOrDefault("rarity_upgrades");
			if (rarityUpgrades is not null) {
				dto.Attributes.Remove("rarity_upgrades");
				item.RarityUpgrades = rarityUpgrades;
			}

			var timestamp = dto.Attributes.GetValueOrDefault("timestamp");
			if (timestamp is not null) {
				dto.Attributes.Remove("timestamp");
				item.Timestamp = timestamp;
			}

			var donatedMuseum = dto.Attributes.GetValueOrDefault("donated_museum");
			if (donatedMuseum is not null) {
				dto.Attributes.Remove("donated_museum");
				item.DonatedMuseum = donatedMuseum;
			}

			item.Attributes = dto.Attributes;
		}

		return item;
	}

	public static ItemDto ToDto(this HypixelItem item) {
		var dto = new ItemDto {
			Uuid = item.Uuid?.ToString(),
			SkyblockId = item.SkyblockId,
			Id = item.Id,
			Damage = item.Damage,
			Count = (byte)item.Count,
			Name = item.Name,
			Lore = item.Lore?.Split('\n').ToList(),
			Enchantments = item.Enchantments,
			Gems = item.Gems,
			ImageUrl = item.Image?.ToPrimaryUrl(),
			Slot = item.Slot,
			Attributes = item.Attributes ?? new Dictionary<string, string>()
		};

		if (item.Modifier is not null) {
			dto.Attributes["modifier"] = item.Modifier;
		}

		if (item.RarityUpgrades is not null) {
			dto.Attributes["rarity_upgrades"] = item.RarityUpgrades;
		}

		if (item.Timestamp is not null) {
			dto.Attributes["timestamp"] = item.Timestamp;
		}

		if (item.DonatedMuseum is not null) {
			dto.Attributes["donated_museum"] = item.DonatedMuseum;
		}
		
		// Remove edition numbers from attributes and lore (replace with obfuscated edition number in lore)
		if (dto.Attributes?.TryGetValue("edition", out var edition) is true) {
            if (dto.Lore is not null) {
				for (var i = 0; i < dto.Lore.Count; i++) {
					dto.Lore[i] = HypixelItemExtensions.EditionRegex().Replace(dto.Lore[i], $"Edition #§khidden§r");
				}
			}
			dto.Attributes.Remove("edition");
		}
		
		// Remove any color attribute to hide exotic colors
		dto.Attributes?.Remove("color");
		
		// Some people care about weird origin tags
		dto.Attributes?.Remove("originTag");

		return dto;
	}
}

public static partial class HypixelItemExtensions
{
	[GeneratedRegex(@$"Edition #?\d+", RegexOptions.IgnoreCase)]
	public static partial Regex EditionRegex();
}
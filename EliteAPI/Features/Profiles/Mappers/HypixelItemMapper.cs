using EliteAPI.Features.Profiles.Models;
using EliteAPI.Models.DTOs.Outgoing;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Profiles.Mappers;

[Mapper]
public static partial class HypixelItemMapper
{
	public static partial HypixelInventoryDto ToDto(this HypixelInventory item);
	
	public static HypixelItem ToHypixelItem(this ItemDto dto)
	{
		var item = new HypixelItem
		{
			Uuid = Guid.TryParse(dto.Uuid, out var guid) ? guid : null,
			SkyblockId = dto.SkyblockId ?? "UNKNOWN",
			Count = dto.Count,
			Name = dto.Name,
			Lore = dto.Lore != null ? string.Join("\n", dto.Lore) : null,
			Enchantments = dto.Enchantments,
			Gems = dto.Gems,
			TextureId = dto.TextureId,
			Slot = dto.Slot,
			LastUpdated = DateTimeOffset.UtcNow
		};

		if (dto.Attributes is not null && dto.Attributes.Count > 0)
		{
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

	public static ItemDto ToDto(this HypixelItem item)
	{
		var dto = new ItemDto
		{
			Uuid = item.Uuid?.ToString(),
			SkyblockId = item.SkyblockId,
			Count = (byte)item.Count,
			Name = item.Name,
			Lore = item.Lore?.Split('\n').ToList(),
			Enchantments = item.Enchantments,
			Gems = item.Gems,
			TextureId = item.TextureId ?? string.Empty,
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
		
		return dto;
	}
}
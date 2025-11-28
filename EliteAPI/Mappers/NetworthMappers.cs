using EliteAPI.Models.DTOs.Outgoing;
using HypixelAPI.Networth.Models;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Mappers;

[Mapper]
public static partial class NetworthMappers
{
	public static NetworthItem ToNetworthItem(this ItemDto item) {
		return new NetworthItem {
			Id = item.Id,
			Count = item.Count,
			Damage = item.Damage,
			SkyblockId = item.SkyblockId,
			Uuid = item.Uuid,
			Name = item.Name,
			Lore = item.Lore,
			Enchantments = item.Enchantments,
			Attributes = item.Attributes != null ? ToNetworthItemAttributes(item.Attributes) : null,
			ItemAttributes = item.ItemAttributes,
			Gems = item.Gems,
			PetInfo = item.PetInfo != null ? ToNetworthItemPetInfo(item.PetInfo) : null,
			TextureId = item.TextureId,
			IsSoulbound = item.Attributes?.Extra?.ContainsKey("coop_soulbound") == true ||
			              item.Attributes?.Extra?.ContainsKey("donated_museum") == true
		};
	}

	private static NetworthItemAttributes ToNetworthItemAttributes(ItemAttributes attributes) {
		return new NetworthItemAttributes {
			Runes = attributes.Runes,
			Effects = attributes.Effects?.Select(e => new NetworthItemEffectAttribute {
				Level = e.Level,
				Effect = e.Effect,
				DurationTicks = e.DurationTicks
			}).ToList(),
			NecromancerSouls = attributes.NecromancerSouls?.Select(s => new NetworthItemSoulAttribute {
				MobId = s.MobId,
				DroppedInstanceId = s.DroppedInstanceId,
				DroppedModeId = s.DroppedModeId
			}).ToList(),
			Hook = attributes.Hook?.ToNetworthDto(),
			Line = attributes.Line?.ToNetworthDto(),
			Sinker = attributes.Sinker?.ToNetworthDto(),
			AbilityScrolls = attributes.AbilityScrolls,
			Inventory = attributes.Inventory?.ToDictionary(k => k.Key, v => v.Value?.ToNetworthItem()),
			Extra = attributes.Extra ?? new Dictionary<string, object>()
		};
	}
	
	public static partial NetworthItemRodPartAttribute ToNetworthDto(this ItemRodPartAttribute rodPartAttribute);

	private static NetworthItemPetInfo ToNetworthItemPetInfo(ItemPetInfoDto petInfo) {
		return new NetworthItemPetInfo {
			Type = petInfo.Type,
			Active = petInfo.Active,
			Exp = petInfo.Exp,
			Level = petInfo.Level,
			Tier = petInfo.Tier,
			CandyUsed = petInfo.CandyUsed,
			HeldItem = petInfo.HeldItem,
			Skin = null // Skin property not available on ItemPetInfoDto
		};
	}
}
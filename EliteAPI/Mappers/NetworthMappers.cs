using EliteAPI.Models.DTOs.Outgoing;
using HypixelAPI.Networth.Models;
using Riok.Mapperly.Abstractions;
using SkyblockRepo;
using SkyblockRepo.Models;

namespace EliteAPI.Mappers;

[Mapper]
public static partial class NetworthMappers
{
	public static NetworthItem ToNetworthItem(this ItemDto item) {
		var repoItem = SkyblockRepoClient.Data.Items.GetValueOrDefault(item.SkyblockId!);
		
		var networthItem = new NetworthItem {
			Id = item.Id,
			Count = item.Count,
			Damage = item.Damage,
			SkyblockId = item.SkyblockId,
			Uuid = item.Uuid,
			Name = item.Name,
			Slot = item.Slot,
			Lore = item.Lore,
			Enchantments = item.Enchantments,
			Attributes = item.Attributes != null ? ToNetworthItemAttributes(item.Attributes) : null,
			ItemAttributes = item.ItemAttributes,
			Gems = item.Gems,
			GemstoneSlots = repoItem?.Data?.GemstoneSlots.ToNetworthDto(),
			PetInfo = item.PetInfo != null ? ToNetworthItemPetInfo(item.PetInfo) : null,
			TextureId = item.TextureId,
			IsSoulbound = item.Attributes?.Extra?.ContainsKey("coop_soulbound") == true ||
			              item.Attributes?.Extra?.ContainsKey("donated_museum") == true,
			IsTradable = true // Evaluate this in the future: repoItem?.Flags is not { Tradable: false, Auctionable: false, Bazaarable: false }
		};

		return networthItem;
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
			Skin = petInfo.Skin
		};
	}

	private static List<NetworthItemGemstoneSlot> ToNetworthDto(this List<ItemGemstoneSlot>? slots) {
		if (slots == null)
		{
			return [];
		}
		return slots.Select(s => new NetworthItemGemstoneSlot {
			SlotType = s.SlotType ?? "UNKNOWN",
			Costs = s.Costs?.Select(costDto => new NetworthItemCost {
				Type = costDto.Type,
				Coins = costDto.Coins,
				ItemId = costDto.ItemId,
				Amount = int.TryParse(costDto.ExtensionData?.ContainsKey("amount") is true
						? costDto.ExtensionData.GetValueOrDefault("amount").ToString()
						: "1",
					out var amount)
					? amount
					: 1
			}).ToList()
		}).ToList();
	}
}
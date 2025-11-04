using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Services;

[RegisterService<VariantKeyGenerator>(LifeTime.Singleton)]
public class VariantKeyGenerator(IOptions<AuctionHouseSettings> settings, ILogger<VariantKeyGenerator> logger)
{
	private readonly List<VariantConfigEntry> _configurations = settings.Value.Variants;
	public const string JoinSeparator = "|";
	private const string MinedCrops = "mined_crops";

	public AuctionItemVariation? Generate(ItemDto itemDto, string rarity) {
		var skyblockId = itemDto.SkyblockId;
		if (string.IsNullOrEmpty(skyblockId)) {
			logger.LogWarning("Cannot generate variant key: SkyblockId is missing from ItemDto");
			return null;
		}

		var variedBy = new AuctionItemVariation();

		if (settings.Value.VaryByRarity.Contains(skyblockId)) variedBy.Rarity = rarity.ToUpperInvariant();

		if (itemDto.PetInfo is not null) {
			variedBy.Rarity = rarity.ToUpperInvariant();
			variedBy.Pet = itemDto.PetInfo.Type;
			variedBy.PetLevel = GenerateFromPetLevel(itemDto);
		}

		// Not checking cultivating because it could be applied to a lot of items and isn't worth the variation
		if (itemDto.Attributes is not null) {
			if (itemDto.Attributes.TryGetValue(MinedCrops, out var minedCrops)) {
				if (long.TryParse(minedCrops, out var minedCrop) && minedCrop > 0) {
					// Get digits for groups, starting from 1,000,000
					var digits = Math.Max(minedCrop.ToString().Length - 6, 0) + 6;
					variedBy.Extra ??= new Dictionary<string, string>();
					variedBy.Extra[MinedCrops] = digits.ToString();
				}
			}

			if (itemDto.Attributes.TryGetValue("party_hat_color", out var partyHatColor)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["party_hat_color"] = partyHatColor;
			}
			
			if (itemDto.Attributes.TryGetValue("party_hat_emoji", out var partyHatEmoji)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["party_hat_emoji"] = partyHatEmoji;
			}

			if (itemDto.SkyblockId is "RUNE" or "UNIQUE_RUNE") {
				if (itemDto.Attributes.Runes is { Count: > 0 } runes) {
					variedBy.Extra ??= new Dictionary<string, string>();
					var applied = itemDto.Attributes.Runes.FirstOrDefault();
					variedBy.Extra["rune"] = applied.Key + ":" + applied.Value;
				}
			}
		}
		
		// if (itemDto.ItemAttributes is not null && itemDto.ItemAttributes.Count > 0)
		// {
		//     variedBy.ItemAttributes = GenerateFromItemAttributes(itemDto);
		// }

		return variedBy;
	}

	[Obsolete("Hypixel removed item attributes, this method is no longer used.")]
	private static Dictionary<string, string>? GenerateFromItemAttributes(ItemDto itemDto) {
		if (itemDto.ItemAttributes == null || itemDto.ItemAttributes.Count == 0) return null;
		var sortedAttributes = itemDto.ItemAttributes
			.OrderBy(kvp => kvp.Key)
			.ToDictionary(k => k.Key.ToLowerInvariant().Replace(":", "-"),
				v => v.Value.ToString().ToLowerInvariant().Replace(":", "-"));

		return sortedAttributes;
	}

	private AuctionItemVariation.PetLevelGroup? GenerateFromPetLevel(ItemDto itemDto) {
		if (itemDto.SkyblockId is null || itemDto.PetInfo is null) return null;

		var petLevelGroups = settings.Value.PetLevelGroups;

		if (settings.Value.PetLevelGroupOverrides.TryGetValue(itemDto.SkyblockId, out var overrideConfig))
			petLevelGroups = overrideConfig;

		var level = itemDto.PetInfo.Level;
		foreach (var (key, group) in petLevelGroups) {
			if (level < group.MinLevel || level > group.MaxLevel) continue;
			return new AuctionItemVariation.PetLevelGroup {
				Key = "LVL_" + group.MinLevel + (group.MaxLevel > group.MinLevel ? "-" + group.MaxLevel : ""),
				Min = group.MinLevel,
				Max = group.MaxLevel
			};
		}

		return null;
	}
}
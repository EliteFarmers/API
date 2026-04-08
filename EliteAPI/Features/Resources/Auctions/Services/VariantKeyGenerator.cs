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

	public AuctionItemVariation? Generate(ItemDto itemDto, string? rarity = null) {
		var skyblockId = itemDto.SkyblockId;
		if (string.IsNullOrEmpty(skyblockId)) {
			logger.LogWarning("Cannot generate variant key: SkyblockId is missing from ItemDto");
			return null;
		}

		var variedBy = new AuctionItemVariation();

		if (settings.Value.VaryByRarity.Contains(skyblockId) && rarity is not null) {
			variedBy.Rarity = rarity.ToUpperInvariant();
		}

		if (itemDto.PetInfo is not null) {
			variedBy.Rarity = itemDto.PetInfo.Tier.ToUpperInvariant();
			variedBy.Pet = itemDto.PetInfo.Type;
			variedBy.PetLevel = GenerateFromPetLevel(itemDto);
		}
		
		// Variant by the single enchantment on enchanted books
		if (skyblockId == "ENCHANTED_BOOK" && itemDto.Enchantments is { Count: 1 }) {
			var enchantment = itemDto.Enchantments.First();
			variedBy.Enchantments = new Dictionary<string, int> {
				{ enchantment.Key.ToUpperInvariant(), enchantment.Value }
			};
		}

		// Variant by potion name and level
		if (skyblockId == "POTION" && itemDto.Attributes is not null) {
			var potionName = itemDto.Attributes["potion_name"] ?? itemDto.Attributes["potion"];
			var potionLevelStr = itemDto.Attributes["potion_level"];
			var potionType = itemDto.Attributes["potion_type"];

			if (!string.IsNullOrEmpty(potionName) && int.TryParse(potionLevelStr, out var potionLevel)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["potion"] = potionName.ToUpperInvariant().Replace(" ", "_") + ":" + potionLevel;
			} else if (!string.IsNullOrEmpty(potionType)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["potion"] = potionType.ToUpperInvariant().Replace(" ", "_");
			}
		}

		// Variant by cake year
		if (skyblockId == "NEW_YEAR_CAKE" && itemDto.Attributes is not null) {
			var cakeYear = itemDto.Attributes["new_years_cake"];
			if (!string.IsNullOrEmpty(cakeYear)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["new_years_cake"] = cakeYear;
			}
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
			
			if (itemDto.Attributes.TryGetValue("baseStatBoostPercentage", out var baseStatBoost) && baseStatBoost == "50") {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["baseStatBoostPercentage"] = baseStatBoost;
			}

			if (itemDto.SkyblockId == "ABICASE" && itemDto.Attributes.TryGetValue("model", out var model)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["model"] = model;
			}

			if (itemDto.Attributes.TryGetValue("party_hat_color", out var partyHatColor)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["party_hat_color"] = partyHatColor;
			}
			
			if (itemDto.Attributes.TryGetValue("party_hat_emoji", out var partyHatEmoji)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["party_hat_emoji"] = partyHatEmoji;
			}
			
			if (itemDto.Attributes.TryGetValue("party_hat_year", out var partyHatYear)) {
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["party_hat_year"] = partyHatYear;
			}

			if (itemDto.SkyblockId is "RUNE" or "UNIQUE_RUNE") {
				if (itemDto.Attributes.Runes is { Count: > 0 } runes) {
					variedBy.Extra ??= new Dictionary<string, string>();
					var applied = itemDto.Attributes.Runes.FirstOrDefault();
					variedBy.Extra["rune"] = applied.Key + ":" + applied.Value;
				}
			}

			if (itemDto.Attributes.AbilityScrolls is { Count: > 0 } abilities) {
				abilities.Sort(StringComparer.InvariantCulture);
				var scrolls = string.Join(JoinSeparator, abilities);
				variedBy.Extra ??= new Dictionary<string, string>();
				variedBy.Extra["scrolls"] = scrolls;
			}
		}

		return variedBy;
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
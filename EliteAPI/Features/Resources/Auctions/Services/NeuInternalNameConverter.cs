using EliteAPI.Features.Resources.Auctions.Models;

namespace EliteAPI.Features.Resources.Auctions.Services;

public static class NeuInternalNameConverter
{
	private static readonly Dictionary<string, int> RarityToNeuId = new(StringComparer.OrdinalIgnoreCase) {
		["COMMON"] = 0,
		["UNCOMMON"] = 1,
		["RARE"] = 2,
		["EPIC"] = 3,
		["LEGENDARY"] = 4,
		["MYTHIC"] = 5
	};

	/// <summary>
	/// Converts an EliteAPI (SkyblockId, VariantKey) pair to a NEU internal name.
	/// Returns null if the item cannot be mapped.
	/// </summary>
	public static string? ToNeuInternalName(string skyblockId, string variantKey) {
		if (string.IsNullOrEmpty(variantKey)) {
			// Items that only exist as variants should not emit a plain SkyblockId
			return skyblockId switch {
				"PET" or "RUNE" or "UNIQUE_RUNE" or "ENCHANTED_BOOK" or "POTION" or "NEW_YEAR_CAKE" or "ABICASE" => null,
				_ => NormalizeSkyblockId(skyblockId)
			};
		}

		var variation = AuctionItemVariation.FromKey(variantKey);

		if (variation.Pet is not null) {
			return ConvertPet(variation);
		}

		if (skyblockId == "ENCHANTED_BOOK" && variation.Enchantments is { Count: 1 }) {
			var enchant = variation.Enchantments.First();
			return $"{enchant.Key.ToUpperInvariant()};{enchant.Value}";
		}

		if (skyblockId is "RUNE" or "UNIQUE_RUNE"
		    && variation.Extra?.TryGetValue("rune", out var runeValue) == true) {
			var parts = runeValue.Split(':', 2);
			if (parts.Length == 2) {
				return $"{parts[0].ToUpperInvariant()}_RUNE;{parts[1]}";
			}
		}

		if (skyblockId == "POTION"
		    && variation.Extra?.TryGetValue("potion", out var potionValue) == true) {
			var parts = potionValue.Split(':', 2);
			return parts.Length == 2
				? $"POTION_{parts[0]};{parts[1]}"
				: $"POTION_{potionValue}";
		}

		if (skyblockId == "NEW_YEAR_CAKE"
		    && variation.Extra?.TryGetValue("new_years_cake", out var cakeYear) == true) {
			return $"NEW_YEAR_CAKE+{cakeYear}";
		}

		if (skyblockId is "PARTY_HAT_CRAB" or "PARTY_HAT_CRAB_ANIMATED") {
			return ConvertCrabHat(variation);
		}

		if (skyblockId == "PARTY_HAT_SLOTH") {
			var emoji = variation.Extra?.GetValueOrDefault("party_hat_emoji")?.ToUpperInvariant();
			return emoji is not null ? $"PARTY_HAT_SLOTH_{emoji}" : null;
		}

		if (skyblockId.StartsWith("BALLOON_HAT_")) {
			return ConvertBalloonHat(variation);
		}

		if (skyblockId == "ABICASE") {
			var model = variation.Extra?.GetValueOrDefault("model")?.ToUpperInvariant();
			return model is not null ? $"ABICASE_{model}" : null;
		}

		// +PERFECT suffix for dungeon items with max base stat boost
		var baseName = NormalizeSkyblockId(skyblockId);
		if (variation.Extra?.GetValueOrDefault("baseStatBoostPercentage") == "50") {
			return baseName + "+PERFECT";
		}

		return baseName;
	}

	private static string? ConvertPet(AuctionItemVariation variation) {
		var rarityNum = RarityToNeuId.GetValueOrDefault(variation.Rarity ?? "COMMON", 0);
		var name = $"{variation.Pet};{rarityNum}";
		
		// LVL_100 = +100, LVL_200 = +200
		// LVL_1-99 and LVL_100-199 = base
		if (variation.PetLevel is not null) {
			if (variation.PetLevel is { Min: 100, Max: 100 })
				name += "+100";
			else if (variation.PetLevel is { Min: 200, Max: 200 })
				name += "+200";
		}

		return name;
	}

	private static string? ConvertCrabHat(AuctionItemVariation variation) {
		var color = variation.Extra?.GetValueOrDefault("party_hat_color")?.ToUpperInvariant();
		if (color is null) return null;

		var year = variation.Extra?.GetValueOrDefault("party_hat_year");
		return year == "2022"
			? $"PARTY_HAT_CRAB_{color}_ANIMATED"
			: $"PARTY_HAT_CRAB_{color}";
	}

	private static string? ConvertBalloonHat(AuctionItemVariation variation) {
		var color = variation.Extra?.GetValueOrDefault("party_hat_color")?.ToUpperInvariant();
		var year = variation.Extra?.GetValueOrDefault("party_hat_year");
		return color is not null && year is not null
			? $"BALLOON_HAT_{year}_{color}"
			: null;
	}

	private static string NormalizeSkyblockId(string skyblockId) {
		return skyblockId.Replace(':', '-');
	}
}

using System.Text.Json;
using System.Text.Json.Serialization;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class PestParser
{
	public static void ParsePestCropCollectionNumbers(this Models.Entities.Farming.Farming farming) {
		var pests = farming.Pests;
		var uncountedCrops = farming.UncountedCrops;

		uncountedCrops[Pest.Mite.GetCrop()] = CalcUncountedCrops(Pest.Mite, pests.Mite);
		uncountedCrops[Pest.Cricket.GetCrop()] = CalcUncountedCrops(Pest.Cricket, pests.Cricket);
		uncountedCrops[Pest.Moth.GetCrop()] = CalcUncountedCrops(Pest.Moth, pests.Moth);
		uncountedCrops[Pest.Earthworm.GetCrop()] = CalcUncountedCrops(Pest.Earthworm, pests.Earthworm);
		uncountedCrops[Pest.Slug.GetCrop()] = CalcUncountedCrops(Pest.Slug, pests.Slug);
		uncountedCrops[Pest.Beetle.GetCrop()] = CalcUncountedCrops(Pest.Beetle, pests.Beetle);
		uncountedCrops[Pest.Locust.GetCrop()] = CalcUncountedCrops(Pest.Locust, pests.Locust);
		uncountedCrops[Pest.Rat.GetCrop()] = CalcUncountedCrops(Pest.Rat, pests.Rat);
		uncountedCrops[Pest.Mosquito.GetCrop()] = CalcUncountedCrops(Pest.Mosquito, pests.Mosquito);
		uncountedCrops[Pest.Fly.GetCrop()] = CalcUncountedCrops(Pest.Fly, pests.Fly);
	}

	public static Dictionary<Crop, long> CalcUncountedCrops(this CropCollection cropCollection) {
		return new Dictionary<Crop, long> {
			[Pest.Mite.GetCrop()] = CalcUncountedCrops(Pest.Mite, cropCollection.Mite),
			[Pest.Cricket.GetCrop()] = CalcUncountedCrops(Pest.Cricket, cropCollection.Cricket),
			[Pest.Moth.GetCrop()] = CalcUncountedCrops(Pest.Moth, cropCollection.Moth),
			[Pest.Earthworm.GetCrop()] = CalcUncountedCrops(Pest.Earthworm, cropCollection.Earthworm),
			[Pest.Slug.GetCrop()] = CalcUncountedCrops(Pest.Slug, cropCollection.Slug),
			[Pest.Beetle.GetCrop()] = CalcUncountedCrops(Pest.Beetle, cropCollection.Beetle),
			[Pest.Locust.GetCrop()] = CalcUncountedCrops(Pest.Locust, cropCollection.Locust),
			[Pest.Rat.GetCrop()] = CalcUncountedCrops(Pest.Rat, cropCollection.Rat),
			[Pest.Mosquito.GetCrop()] = CalcUncountedCrops(Pest.Mosquito, cropCollection.Mosquito),
			[Pest.Fly.GetCrop()] = CalcUncountedCrops(Pest.Fly, cropCollection.Fly)
		};
	}

	public static long CalcUncountedCrops(Pest pest, int kills, ConfigFarmingWeightSettings? weightConfig = null) {
		var config = weightConfig ?? FarmingWeightConfig.Settings;
		var pestBrackets = config.PestDropBracketsList;
		var pestDropChances = config.PestCropDropChances;

		var pestCount = kills;
		var pestsToCount = 0;
		var totalDrops = 0;

		for (var i = 0; i < pestBrackets.Count; i++) {
			// Exit if there are no more pests to calculate
			if (pestCount <= 0) break;

			var fortune = pestBrackets[i].Value;
			var pestDrops = pestDropChances[pest];

			// Use the last bracket for all remaining pests
			if (i == pestBrackets.Count - 1) {
				totalDrops +=
					(int)Math.Ceiling(pestDrops.GetCropsToSubtract(fortune, weightConfig: config) * pestCount);
				continue;
			}

			// Get the next bracket to find the maximum pests in the current bracket
			var nextBracket = pestBrackets.ElementAtOrDefault(i + 1).Key;
			if (nextBracket is null) break;

			if (!int.TryParse(nextBracket, out var maxPestsInBracket)) continue;

			// Calculate the pests to count in the current bracket
			pestsToCount = Math.Min(maxPestsInBracket - pestsToCount, pestCount);

			if (fortune == 0) {
				// If the fortune is 0, we don't need to calculate the drops
				pestCount -= pestsToCount;
				continue;
			}

			// Calculate the drops for the current bracket
			var drops = pestDrops.GetCropsToSubtract(fortune, weightConfig: config) * pestsToCount;

			pestCount -= pestsToCount;
			totalDrops += (int)Math.Ceiling(drops);
		}

		return totalDrops;
	}

	public static void ParsePests(this ProfileMember member, ProfileMemberResponse memberData) {
		if (memberData.Bestiary is null) return;

		memberData.Bestiary.TryGetPropertyValue("kills", out var kills);
		if (kills is null) return;

		try {
			var bestiaryKills = kills.Deserialize<BestiaryKillsMapping>() ?? new BestiaryKillsMapping();
			var pests = member.Farming.Pests;

			pests.Beetle = bestiaryKills.Beetle;
			pests.Cricket = bestiaryKills.Cricket;
			pests.Fly = bestiaryKills.Fly;
			pests.Locust = bestiaryKills.Locust;
			pests.Mite = bestiaryKills.Mite;
			pests.Mosquito = bestiaryKills.Mosquito;
			pests.Moth = bestiaryKills.Moth;
			pests.Rat = bestiaryKills.Rat;
			pests.Slug = bestiaryKills.Slug;
			pests.Earthworm = bestiaryKills.Earthworm;
			pests.Mouse = bestiaryKills.Mouse;
		}
		catch (JsonException) {
			return;
		}

		member.Farming.ParsePestCropCollectionNumbers();
	}

	public static Pest GetPest(this Crop crop) {
		if (crop == Crop.Seeds) return Pest.Fly;
		return (Pest)crop;
	}

	public static Crop GetCrop(this Pest pest) {
		return (Crop)pest;
	}

	private class BestiaryKillsMapping
	{
		[JsonPropertyName("pest_mite_1")] public int Mite { get; set; } = 0;
		[JsonPropertyName("pest_cricket_1")] public int Cricket { get; set; } = 0;
		[JsonPropertyName("pest_moth_1")] public int Moth { get; set; } = 0;
		[JsonPropertyName("pest_worm_1")] public int Earthworm { get; set; } = 0;
		[JsonPropertyName("pest_slug_1")] public int Slug { get; set; } = 0;
		[JsonPropertyName("pest_beetle_1")] public int Beetle { get; set; } = 0;
		[JsonPropertyName("pest_locust_1")] public int Locust { get; set; } = 0;
		[JsonPropertyName("pest_rat_1")] public int Rat { get; set; } = 0;
		[JsonPropertyName("pest_mosquito_1")] public int Mosquito { get; set; } = 0;
		[JsonPropertyName("pest_fly_1")] public int Fly { get; set; } = 0;
		[JsonPropertyName("pest_mouse_1")] public int Mouse { get; set; } = 0;
	}
}
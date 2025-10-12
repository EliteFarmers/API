using System.Text.Json;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Utilities;

namespace EliteAPI.Parsers.Farming;

public static class CropCollectionParser {
	public static Dictionary<Crop, long> ExtractCropCollections(this ProfileMember member, bool includeSeeds = false) {
		return member.Collections.ExtractCropCollections(includeSeeds);
	}

	public static Dictionary<Crop, long> ExtractCropCollections(this Dictionary<string, long> collections,
		bool includeSeeds = false) {
		try {
			var crops = new Dictionary<Crop, long>();

			foreach (var cropId in FarmingWeightConfig.Settings.CropItemIds) {
				var crop = FormatUtils.GetCropFromItemId(cropId);
				if (crop is null) continue;

				collections.TryGetValue(cropId, out var amount);

				crops.Add(crop.Value, amount);
			}

			if (!includeSeeds) return crops;

			var seeds = collections.TryGetValue("SEEDS", out var seedCollection) ? seedCollection : 0;
			crops.Add(Crop.Seeds, seeds);

			return crops;
		}
		catch (Exception e) {
			Console.Error.WriteLine(e);
			return new Dictionary<Crop, long>();
		}
	}

	public static Dictionary<Crop, long> ExtractCollectionIncreases(this Dictionary<Crop, long> initialCollections,
		Dictionary<Crop, long> currentCollections) {
		var cropIncreases = new Dictionary<Crop, long>();
		if (currentCollections is null or { Count: 0 } || initialCollections is { Count: 0 }) return cropIncreases;

		foreach (var crop in currentCollections.Keys) {
			if (!initialCollections.TryGetValue(crop, out var initialAmount)) continue;

			var currentAmount = currentCollections[crop];

			var increase = Math.Max(currentAmount - initialAmount, 0);

			cropIncreases.Add(crop, increase);
		}

		return cropIncreases;
	}

	public static Dictionary<string, long> ExtractReadableCropCollections(this CropCollection cropCollection) {
		return new Dictionary<string, long> {
			{ "cactus", cropCollection.Cactus },
			{ "carrot", cropCollection.Carrot },
			{ "cocoa", cropCollection.CocoaBeans },
			{ "melon", cropCollection.Melon },
			{ "mushroom", cropCollection.Mushroom },
			{ "wart", cropCollection.NetherWart },
			{ "potato", cropCollection.Potato },
			{ "pumpkin", cropCollection.Pumpkin },
			{ "cane", cropCollection.SugarCane },
			{ "wheat", cropCollection.Wheat },
			{ "seeds", cropCollection.Seeds }
		};
	}

	public static Dictionary<string, int> ExtractPestKills(this CropCollection cropCollection) {
		return new Dictionary<string, int> {
			{ "mite", cropCollection.Mite },
			{ "cricket", cropCollection.Cricket },
			{ "moth", cropCollection.Moth },
			{ "worm", cropCollection.Earthworm },
			{ "slug", cropCollection.Slug },
			{ "beetle", cropCollection.Beetle },
			{ "locust", cropCollection.Locust },
			{ "rat", cropCollection.Rat },
			{ "mosquito", cropCollection.Mosquito },
			{ "fly", cropCollection.Fly },
			{ "mouse", cropCollection.Mouse }
		};
	}
}
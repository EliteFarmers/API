using EliteAPI.Models.Entities.Hypixel;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class GardenParser
{
	public static Dictionary<string, VisitorData> CombineVisitors(this GardenResponseData data) {
		var visitors = data.Visitors;

		var result = new Dictionary<string, VisitorData>();
		if (visitors is null) return result;

		foreach (var (id, visitCount) in visitors.Visits) {
			var accepted = visitors.Completed.TryGetValue(id, out var acceptedCount) ? acceptedCount : 0;
			var currentlyVisiting = data.CurrentVisitors.ContainsKey(id);

			var visitorData = new VisitorData {
				Visits = currentlyVisiting ? visitCount - 1 : visitCount,
				Accepted = accepted
			};

			if (visitorData is { Visits: 0, Accepted: 0 }) continue;

			result[id] = visitorData;
		}

		return result;
	}

	public static void PopulateCropUpgrades(this Garden garden, GardenResponseData data) {
		var incoming = data.CropUpgrades;
		var upgrades = garden.Upgrades;

		upgrades.Cactus = incoming.TryGetValue(CropId.Cactus, out var cactus) ? cactus : upgrades.Cactus;
		upgrades.Carrot = incoming.TryGetValue(CropId.Carrot, out var carrot) ? carrot : upgrades.Carrot;
		upgrades.CocoaBeans = incoming.TryGetValue(CropId.CocoaBeans, out var cocoa) ? cocoa : upgrades.CocoaBeans;
		upgrades.Melon = incoming.TryGetValue(CropId.Melon, out var melon) ? melon : upgrades.Melon;
		upgrades.Mushroom = incoming.TryGetValue(CropId.Mushroom, out var mushroom) ? mushroom : upgrades.Mushroom;
		upgrades.NetherWart = incoming.TryGetValue(CropId.NetherWart, out var netherWart)
			? netherWart
			: upgrades.NetherWart;
		upgrades.Potato = incoming.TryGetValue(CropId.Potato, out var potato) ? potato : upgrades.Potato;
		upgrades.Pumpkin = incoming.TryGetValue(CropId.Pumpkin, out var pumpkin) ? pumpkin : upgrades.Pumpkin;
		upgrades.SugarCane = incoming.TryGetValue(CropId.SugarCane, out var sugarCane) ? sugarCane : upgrades.SugarCane;
		upgrades.Wheat = incoming.TryGetValue(CropId.Wheat, out var wheat) ? wheat : upgrades.Wheat;
		upgrades.Sunflower = incoming.TryGetValue(CropId.Sunflower, out var sunflower) ? sunflower : upgrades.Sunflower;
		upgrades.Moonflower = incoming.TryGetValue(CropId.Moonflower, out var moonflower) ? moonflower : upgrades.Moonflower;
		upgrades.WildRose = incoming.TryGetValue(CropId.WildRose, out var wildRose) ? wildRose : upgrades.WildRose;
	}

	public static void PopulateCropMilestones(this Garden garden, GardenResponseData data) {
		var incoming = data.CropMilestones;
		var upgrades = garden.Crops;

		upgrades.Cactus = incoming.TryGetValue(CropId.Cactus, out var cactus) ? cactus : upgrades.Cactus;
		upgrades.Carrot = incoming.TryGetValue(CropId.Carrot, out var carrot) ? carrot : upgrades.Carrot;
		upgrades.CocoaBeans = incoming.TryGetValue(CropId.CocoaBeans, out var cocoa) ? cocoa : upgrades.CocoaBeans;
		upgrades.Melon = incoming.TryGetValue(CropId.Melon, out var melon) ? melon : upgrades.Melon;
		upgrades.Mushroom = incoming.TryGetValue(CropId.Mushroom, out var mushroom) ? mushroom : upgrades.Mushroom;
		upgrades.NetherWart = incoming.TryGetValue(CropId.NetherWart, out var netherWart)
			? netherWart
			: upgrades.NetherWart;
		upgrades.Potato = incoming.TryGetValue(CropId.Potato, out var potato) ? potato : upgrades.Potato;
		upgrades.Pumpkin = incoming.TryGetValue(CropId.Pumpkin, out var pumpkin) ? pumpkin : upgrades.Pumpkin;
		upgrades.SugarCane = incoming.TryGetValue(CropId.SugarCane, out var sugarCane) ? sugarCane : upgrades.SugarCane;
		upgrades.Wheat = incoming.TryGetValue(CropId.Wheat, out var wheat) ? wheat : upgrades.Wheat;
		upgrades.Sunflower = incoming.TryGetValue(CropId.Sunflower, out var sunflower) ? sunflower : upgrades.Sunflower;
		upgrades.Moonflower = incoming.TryGetValue(CropId.Moonflower, out var moonflower) ? moonflower : upgrades.Moonflower;
		upgrades.WildRose = incoming.TryGetValue(CropId.WildRose, out var wildRose) ? wildRose : upgrades.WildRose;
	}
}
using EliteAPI.Models.Entities.Hypixel;
using HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class GardenParser {
	public static Dictionary<string, VisitorData> CombineVisitors(this GardenVisitorData vistors) {
		var result = new Dictionary<string, VisitorData>();
		
		foreach (var (id, visitCount) in vistors.Visits) {
			var accepted = vistors.Completed.TryGetValue(id, out var acceptedCount) ? acceptedCount : 0;
			
			var data = new VisitorData {
				Visits = visitCount,
				Accepted = accepted
			};
			
			result[id] = data;
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
		upgrades.NetherWart = incoming.TryGetValue(CropId.NetherWart, out var netherWart) ? netherWart : upgrades.NetherWart;
		upgrades.Potato = incoming.TryGetValue(CropId.Potato, out var potato) ? potato : upgrades.Potato;
		upgrades.Pumpkin = incoming.TryGetValue(CropId.Pumpkin, out var pumpkin) ? pumpkin : upgrades.Pumpkin;
		upgrades.SugarCane = incoming.TryGetValue(CropId.SugarCane, out var sugarCane) ? sugarCane : upgrades.SugarCane;
		upgrades.Wheat = incoming.TryGetValue(CropId.Wheat, out var wheat) ? wheat : upgrades.Wheat;
	}

	public static void PopulateCropMilestones(this Garden garden, GardenResponseData data) {
		var incoming = data.CropMilestones;
		var upgrades = garden.Crops;
		
		upgrades.Cactus = incoming.TryGetValue(CropId.Cactus, out var cactus) ? cactus : upgrades.Cactus;
		upgrades.Carrot = incoming.TryGetValue(CropId.Carrot, out var carrot) ? carrot : upgrades.Carrot;
		upgrades.CocoaBeans = incoming.TryGetValue(CropId.CocoaBeans, out var cocoa) ? cocoa : upgrades.CocoaBeans;
		upgrades.Melon = incoming.TryGetValue(CropId.Melon, out var melon) ? melon : upgrades.Melon;
		upgrades.Mushroom = incoming.TryGetValue(CropId.Mushroom, out var mushroom) ? mushroom : upgrades.Mushroom;
		upgrades.NetherWart = incoming.TryGetValue(CropId.NetherWart, out var netherWart) ? netherWart : upgrades.NetherWart;
		upgrades.Potato = incoming.TryGetValue(CropId.Potato, out var potato) ? potato : upgrades.Potato;
		upgrades.Pumpkin = incoming.TryGetValue(CropId.Pumpkin, out var pumpkin) ? pumpkin : upgrades.Pumpkin;
		upgrades.SugarCane = incoming.TryGetValue(CropId.SugarCane, out var sugarCane) ? sugarCane : upgrades.SugarCane;
		upgrades.Wheat = incoming.TryGetValue(CropId.Wheat, out var wheat) ? wheat : upgrades.Wheat;
	}
}
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Utilities;
using EliteFarmers.HypixelAPI.DTOs;

namespace EliteAPI.Parsers.Farming;

public static class CropParser
{
	public static string SimpleName(this Crop crop) {
		return crop switch {
			Crop.Cactus => "cactus",
			Crop.Carrot => "carrot",
			Crop.CocoaBeans => "cocoa",
			Crop.Melon => "melon",
			Crop.Mushroom => "mushroom",
			Crop.NetherWart => "wart",
			Crop.Potato => "potato",
			Crop.Pumpkin => "pumpkin",
			Crop.SugarCane => "cane",
			Crop.Wheat => "wheat",
			Crop.Seeds => "seeds",
			Crop.Sunflower => "sunflower",
			Crop.Moonflower => "moonflower",
			Crop.WildRose => "wildrose",
			_ => throw new ArgumentOutOfRangeException(nameof(crop), crop, null)
		};
	}

	public static string ProperName(this Crop crop) {
		return FormatUtils.GetFormattedCropName(crop);
	}

	public static bool TryGetCrop(this string itemId, out Crop crop) {
		Crop? result = itemId switch {
			CropId.Cactus => Crop.Cactus,
			CropId.Carrot => Crop.Carrot,
			CropId.Melon => Crop.Melon,
			CropId.Mushroom => Crop.Mushroom,
			CropId.NetherWart => Crop.NetherWart,
			CropId.Potato => Crop.Potato,
			CropId.Pumpkin => Crop.Pumpkin,
			CropId.SugarCane => Crop.SugarCane,
			CropId.Wheat => Crop.Wheat,
			CropId.Seeds => Crop.Seeds,
			CropId.Sunflower => Crop.Sunflower,
			CropId.Moonflower => Crop.Moonflower,
			CropId.WildRose => Crop.WildRose,
			CropId.CocoaBeans or CropId.CocoaBeansAlt or CropId.CocoaBeansAlt2 => Crop.CocoaBeans,
			_ => null
		};

		if (result != null) {
			crop = result.Value;
			return true;
		}

		crop = default;
		return false;
	}

	public static readonly Dictionary<string, string> CarrolynMappings = new Dictionary<string, string>() {
		// { CropId.Cactus, "CARROLYN_EXPORT_CROP_" },
		{ CropId.Carrot, "CARROLYN_EXPORT_CROP_EXPORTABLE_CARROTS" },
		{ CropId.CocoaBeans, "CARROLYN_EXPORT_CROP_SUPREME_CHOCOLATE_BAR" },
		// { CropId.Melon, "CARROLYN_EXPORT_CROP_" },
		{ CropId.Mushroom, "CARROLYN_EXPORT_CROP_HALF_EATEN_MUSHROOM" },
		{ CropId.NetherWart, "CARROLYN_EXPORT_CROP_WARTY" },
		// { CropId.Potato, "CARROLYN_EXPORT_CROP_" },
		{ CropId.Pumpkin, "CARROLYN_EXPORT_CROP_EXPIRED_PUMPKIN" },
		// { CropId.SugarCane, "CARROLYN_EXPORT_CROP_" },
		{ CropId.Wheat, "CARROLYN_EXPORT_CROP_FINE_FLOUR" },
		// { CropId.Sunflower, "CARROLYN_EXPORT_CROP_" },
		// { CropId.Moonflower, "CARROLYN_EXPORT_CROP_" },
		{ CropId.WildRose, "CARROLYN_EXPORT_CROP_PRICKLY_KISS" }
	};

	public static Dictionary<string, bool>? GetExportedCrops(this ProfileMemberResponse member) {
		var completedTasks = member.Leveling?.CompletedTasks;
		if (completedTasks is null) return null;
		
		var exportedCrops = new Dictionary<string, bool>();
		
		foreach (var mapping in CarrolynMappings) {
			var isExported = completedTasks.Contains(mapping.Value);
			if (!isExported) continue;
			exportedCrops.TryAdd(mapping.Key, isExported);
		}
		
		return exportedCrops;
	}

	public static int GetDnaMilestone(this ProfileMemberResponse member) {
		var completedTasks = member.Objectives?.Tutorial;
		if (completedTasks is null) return 0;
		
		var level = 0;
		foreach (var task in completedTasks) {
			if (!task.StartsWith("dna_analysis_rewardfarming_fortune_")) continue;
			var number = task.Replace("dna_analysis_rewardfarming_fortune_", "");
			if (int.TryParse(number, out var parsedNumber)) {
				level = Math.Max(level, parsedNumber);
			}
		}
		
		return level;
	}
}

public static class CropId
{
	public const string Cactus = "CACTUS";
	public const string Carrot = "CARROT_ITEM";
	public const string CocoaBeans = "INK_SACK:3";

	/// <summary>
	/// Needed because config files can't have colons in keys
	/// </summary>
	public const string CocoaBeansAlt = "INK_SACK_3";

	/// <summary>
	/// Used in Jacob contest key parsing
	/// </summary>
	public const string CocoaBeansAlt2 = "INK_SACK";

	public const string Melon = "MELON";
	public const string Mushroom = "MUSHROOM_COLLECTION";
	public const string NetherWart = "NETHER_STALK";
	public const string Potato = "POTATO_ITEM";
	public const string Pumpkin = "PUMPKIN";
	public const string SugarCane = "SUGAR_CANE";
	public const string Wheat = "WHEAT";
	public const string Seeds = "SEEDS";
	public const string Sunflower = "DOUBLE_PLANT";
	public const string Moonflower = "MOONFLOWER";
	public const string WildRose = "WILD_ROSE";
}
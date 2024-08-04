using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Utilities;

namespace EliteAPI.Parsers.Farming;

public static class CropParser {
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
}

public static class CropId {
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
}
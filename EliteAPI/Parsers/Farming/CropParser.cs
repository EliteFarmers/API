using EliteAPI.Models.Entities.Hypixel;

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
}
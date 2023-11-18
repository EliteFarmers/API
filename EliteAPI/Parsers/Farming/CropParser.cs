﻿using System.Diagnostics.CodeAnalysis;
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

    public static bool TryGetCrop(this string itemId, out Crop crop) {
        Crop? result = itemId switch {
            "CACTUS" => Crop.Cactus,
            "CARROT_ITEM" => Crop.Carrot,
            "INK_SACK:3" => Crop.CocoaBeans,
            "MELON" => Crop.Melon,
            "MUSHROOM_COLLECTION" => Crop.Mushroom,
            "NETHER_STALK" => Crop.NetherWart,
            "POTATO_ITEM" => Crop.Potato,
            "PUMPKIN" => Crop.Pumpkin,
            "SUGAR_CANE" => Crop.SugarCane,
            "WHEAT" => Crop.Wheat,
            "WHEAT_SEEDS" => Crop.Seeds,
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
using System.Text.Json;
using EliteAPI.Configuration.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Tests.EventProgressTests; 

public class CollectionTests {

    public CollectionTests() {
        // Populate the crop item ids
        FarmingWeightConfig.Settings.CropItemIds =
        [
            "CACTUS",
            "CARROT_ITEM",
            "INK_SACK:3", // Cocoa
            "WHEAT",
            "SUGAR_CANE",
            "POTATO_ITEM",
            "PUMPKIN",
            "MELON",
            "NETHER_STALK",
            "MUSHROOM_COLLECTION"
        ];
    }
    
    [Fact]
    public void ExtractCropCollectionsTest() {
        var input = new Dictionary<string, long> {
            { "CACTUS", 1021921 },
            { "CARROT_ITEM", 124738 },
            { "INK_SACK:3", 120182 }, // Cocoa
            { "WHEAT", 9573 },
            { "SUGAR_CANE", 0 }, 
            { "POTATO_ITEM", 0 },
            { "PUMPKIN", 12742 },
            { "MELON", 7666 },
            { "NETHER_STALK", 29982192031 },
            { "MUSHROOM_COLLECTION", 210 },
            { "SEEDS", 12 } // Should be skipped
        };
        
        var expected = new Dictionary<Crop, long> {
            { Crop.Cactus, 1021921 },
            { Crop.Carrot, 124738 },
            { Crop.CocoaBeans, 120182 },
            { Crop.Wheat, 9573 },
            { Crop.SugarCane, 0 },
            { Crop.Potato, 0 },
            { Crop.Pumpkin, 12742 },
            { Crop.Melon, 7666 },
            { Crop.NetherWart, 29982192031 },
            { Crop.Mushroom, 210 }
        };
        
        var jsonDoc = JsonSerializer.SerializeToDocument(input);
        var actual = jsonDoc.ExtractCropCollections();

        actual.ShouldBe(expected);
    }
    
    [Fact]
    public void ExtractCropCollectionsWithMissingTest() {
        var input = new Dictionary<string, long> {
            { "CACTUS", 1021921 },
            { "CARROT_ITEM", 124738 },
            { "INK_SACK:3", 120182 }, // Cocoa
            { "PUMPKIN", 12742 },
            { "MELON", 7666 },
            { "NETHER_STALK", 29982192031 },
            { "SEEDS", 12 }
        };
        
        var expected = new Dictionary<Crop, long> {
            { Crop.Cactus, 1021921 },
            { Crop.Carrot, 124738 },
            { Crop.CocoaBeans, 120182 },
            { Crop.Wheat, 0 },
            { Crop.SugarCane, 0 },
            { Crop.Potato, 0 },
            { Crop.Pumpkin, 12742 },
            { Crop.Melon, 7666 },
            { Crop.NetherWart, 29982192031 },
            { Crop.Mushroom, 0 }
        };
        
        var jsonDoc = JsonSerializer.SerializeToDocument(input);
        var actual = jsonDoc.ExtractCropCollections();
        
        actual.ShouldBe(expected);
        
        var withSeeds = jsonDoc.ExtractCropCollections(true);
        expected.Add(Crop.Seeds, 12);
        
        withSeeds.ShouldBe(expected);
    }
}
using System.Text.Json;
using EliteAPI.Config.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Utilities;

namespace EliteAPI.Parsers.Farming; 

public static class CropCollectionParser {

    public static Dictionary<Crop, long> ExtractCropCollections(this ProfileMember member) {
        var collections = member.Collections.Deserialize<Dictionary<string, long>>() ?? new Dictionary<string, long>();
        var crops = new Dictionary<Crop, long>();

        foreach (var cropId in FarmingWeightConfig.Settings.CropItemIds)
        {
            var crop = FormatUtils.GetCropFromItemId(cropId);
            if (crop is null) continue;

            collections.TryGetValue(cropId, out var amount);

            crops.Add(crop.Value, amount);
        }

        return crops;    
    }
    
    public static Dictionary<string, long> ExtractCropCollections(this CropCollection cropCollection) {
        return new Dictionary<string, long> {
            { "cactus", cropCollection.Cactus },
            { "carrot", cropCollection.Carrot },
            { "cocoaBeans", cropCollection.CocoaBeans },
            { "melon", cropCollection.Melon },
            { "mushroom", cropCollection.Mushroom },
            { "netherWart", cropCollection.NetherWart },
            { "potato", cropCollection.Potato },
            { "pumpkin", cropCollection.Pumpkin },
            { "sugarCane", cropCollection.SugarCane },
            { "wheat", cropCollection.Wheat },
        };
    }
    
}
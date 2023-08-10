using System.Text.Json;
using EliteAPI.Config.Settings;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Timescale;
using EliteAPI.Utilities;

namespace EliteAPI.Parsers.Farming; 

public static class CropCollectionParser {

    public static Dictionary<Crop, long> ExtractCropCollections(this ProfileMember member, bool includeSeeds = false) {
        return member.Collections.ExtractCropCollections(includeSeeds);
    }

    public static Dictionary<Crop, long> ExtractCropCollections(this JsonDocument collectionDocument, bool includeSeeds = false) {
        try {
            var collections = collectionDocument.Deserialize<Dictionary<string, long>>() ??
                              new Dictionary<string, long>();
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
        };
    }
    
}
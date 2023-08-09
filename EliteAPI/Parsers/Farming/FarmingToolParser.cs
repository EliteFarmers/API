using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Parsers.Farming; 

public static class FarmingToolParser {
    
    public static Dictionary<string, long> ToMapOfCollectedItems(this Models.Entities.Farming.Farming farming, Dictionary<string, long>? existing = null) {
        var tools = existing ?? new Dictionary<string, long>();
        
        if (farming.Inventory?.Tools is null or { Count: 0 }) return tools;
        
        foreach (var item in farming.Inventory.Tools)
        {
            var uuid = item.Uuid;
            if (uuid.IsNullOrEmpty()) continue;
            
            // Skip if the item is already in the dictionary
            if (existing?.ContainsKey(uuid!) is true) continue;

            var collected = item.ExtractCollected();
            if (collected == 0) continue;
            
            tools.Add(uuid!, collected);
        }
        
        return tools;
    }
    
    public static Crop? ExtractCrop(this ItemDto tool) {
        var toolIds = FarmingItemsConfig.Settings.FarmingToolIds;
        
        if (tool.SkyblockId is null) return null;
        if (!toolIds.ContainsKey(tool.SkyblockId)) return null;
        
        return toolIds[tool.SkyblockId];
    }

    public static long ExtractCollected(this ItemDto tool) {
        if (tool.Attributes?.TryGetValue("mined_crops", out var collected) is true 
            && long.TryParse(collected, out var mined)) {
            return mined;
        }
            
        if (tool.Attributes?.TryGetValue("farmed_cultivating", out var cultivated) is true 
            && long.TryParse(cultivated, out var crops)) {
            return crops;
        }
        
        return 0;
    }
}
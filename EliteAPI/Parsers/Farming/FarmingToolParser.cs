using Microsoft.IdentityModel.Tokens;

namespace EliteAPI.Mappers.Farming; 

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
            
            if (long.TryParse(item.Attributes?["mined_crops"], out var collected)) {
                tools.Add(uuid!, collected);
                continue;
            }
            
            if (long.TryParse(item.Attributes?["farmed_cultivating"], out var cultivated)) {
                tools.Add(uuid!, cultivated);
            }
        }
        
        return tools;
    }
}
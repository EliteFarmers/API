using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Config.Settings; 

public class FarmingItemsSettings {
    public Dictionary<string, Crop> FarmingToolIds { get; set; } = new();
}
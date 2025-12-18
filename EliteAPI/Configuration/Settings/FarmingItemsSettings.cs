using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Configuration.Settings;

public class FarmingItemsConfig
{
	public static FarmingItemsSettings Settings { get; set; } = new();
}

public class FarmingItemsSettings
{
	public Dictionary<string, Crop> FarmingToolIds { get; set; } = new();
	public Dictionary<string, short> FarmingEquipmentIds { get; set; } = new();
	public Dictionary<string, short> FarmingArmorIds { get; set; } = new();
	public Dictionary<string, short> FarmingAccessoryIds { get; set; } = new();
	public List<string> ShardIds { get; set; } = [];
	public List<string> ChipIds { get; set; } = [];
}
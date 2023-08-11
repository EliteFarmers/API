using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Config.Settings;

public static class FarmingWeightConfig
{
    // Accessing config like this is not recommended, but it's the only way (that i know of) to do it without DI in a static class
    public static ConfigFarmingWeightSettings Settings { get; set; } = new();
}

public class ConfigFarmingWeightSettings
{
    public List<string> CropItemIds { get; set; } = new();
    public List<string> FarmingMinions { get; set; } = new();
    public Dictionary<string, double> CropsPerOneWeight { get; set; } = new();
    public Dictionary<string, double> EventCropsPerOneWeight { get; set; } = new();
    
    public int Farming60Bonus { get; set; }
    public int Farming50Bonus { get; set; }
    public int AnitaBuffBonusMultiplier { get; set; }
    public int MaxMedalsCounted { get; set; }
    public int GoldMedalRewardInterval { get; set; }
    public float WeightPerGoldMedal { get; set; }
    public int MinionRewardTier { get; set; }
    public int MinionRewardWeight { get; set; }
}
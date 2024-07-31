using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Configuration.Settings;

public static class FarmingWeightConfig
{
    // Accessing config like this is not recommended, but it's the only way (that i know of) to do it without DI in a static class
    public static ConfigFarmingWeightSettings Settings { get; set; } = new();
}

public class ConfigFarmingWeightSettings
{
    public List<string> CropItemIds { get; set; } = new();
    public List<string> FarmingMinions { get; set; } = new();

    public Dictionary<Crop, double> CropWeights { get; set; } = new();
    private Dictionary<string, double> _cropsPerOneWeight = new();
    public Dictionary<string, double> CropsPerOneWeight {
        get => _cropsPerOneWeight; 
        set {
            _cropsPerOneWeight = value;
            foreach (var (key, v) in _cropsPerOneWeight) {
                if (key.TryGetCrop(out var crop)) {
                    CropWeights[crop] = v;
                }
            }
        } 
    }

    public Dictionary<Crop, double> EventCropWeights { get; set; } = new();
    private Dictionary<string, double> _eventCropsPerOneWeight = new();
    public Dictionary<string, double> EventCropsPerOneWeight {
        get => _eventCropsPerOneWeight;
        set {
            _eventCropsPerOneWeight = value;
            foreach (var (key, v) in _eventCropsPerOneWeight) {
                if (key.TryGetCrop(out var crop)) {
                    EventCropWeights[crop] = v;
                }
            }
        } 
    }
    public int Farming60Bonus { get; set; }
    public int Farming50Bonus { get; set; }
    public int AnitaBuffBonusMultiplier { get; set; }
    public int MaxMedalsCounted { get; set; }
    public float WeightPerDiamondMedal { get; set; }
    public float WeightPerPlatinumMedal { get; set; }
    public float WeightPerGoldMedal { get; set; }
    public int MinionRewardTier { get; set; }
    public int MinionRewardWeight { get; set; }
}
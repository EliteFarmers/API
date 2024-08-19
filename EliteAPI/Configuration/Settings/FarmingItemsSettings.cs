using System.Text.Json.Serialization;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Configuration.Settings; 

public class FarmingItemsConfig {
    public static FarmingItemsSettings Settings { get; set; } = new();
}

public class FarmingItemsSettings {
    public Dictionary<string, Crop> FarmingToolIds { get; set; } = new();
    public Dictionary<string, short> FarmingEquipmentIds { get; set; } = new();
    public Dictionary<string, short> FarmingArmorIds { get; set; } = new();
    public Dictionary<string, short> FarmingAccessoryIds { get; set; } = new();
    public Dictionary<Pest, string> PestIds { get; set; } = new();
    
    [JsonIgnore]
    private Dictionary<string, int> _pestDropBrackets = new();
    public Dictionary<string, int> PestDropBrackets {
        get => _pestDropBrackets;
        set {
            _pestDropBrackets = value;
            PestDropBracketsList = value.ToList();
        }
    }
    public List<KeyValuePair<string, int>> PestDropBracketsList = [];
    
    public Dictionary<Pest, PestDropChance> PestCropDropChances { get; set; } = new();
}

public class PestDropChance {
    public int Base { get; set; } = 0;
    public List<PestRngDrop> Rare { get; set; } = [];
    
    [JsonIgnore]
    private Dictionary<int, double> Precomputed { get; } = new();
    
    public double GetCropsDropped(int fortune, bool includeZero = false, bool usePrecomputed = true) {
        if (usePrecomputed && Precomputed.TryGetValue(fortune, out var chance)) return chance;

        // Zero fortune means we're ignoring the drops from this bracket
        if (fortune == 0 && !includeZero) {
            if (usePrecomputed) {
                Precomputed[fortune] = 0;
            }
            return 0;
        }
        
        var drops = Base * (fortune / 100f + 1);
        var rng = Rare.Sum((r) => r.Chance * (fortune / 600f + 1) * r.Drops);

        if (!usePrecomputed) {
            return drops + rng;
        }
        
        Precomputed[fortune] = drops + rng;
        return Precomputed[fortune];
    }
    
    public Dictionary<int, double> GetPrecomputed() {
        if (Precomputed.Count >= FarmingItemsConfig.Settings.PestDropBrackets.Count) return Precomputed;
        
        var pestBrackets = FarmingItemsConfig.Settings.PestDropBrackets;
        foreach (var fortune in pestBrackets.Values) {
            GetCropsDropped(fortune);
        }
        
        return Precomputed;
    }
}

public class PestRngDrop {
    public int Drops { get; set; } = 0;
    public double Chance { get; set; } = 0;
}
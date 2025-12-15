using System.Collections.Concurrent;
using System.Text.Json.Serialization;
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
	public List<string> CropItemIds { get; set; } = [];
	public List<string> FarmingMinions { get; set; } = [];
	public int MinimumWeightForTracking { get; set; } = 1_000;
	public Dictionary<Crop, double> CropWeights { get; set; } = new();
	private Dictionary<string, double> _cropsPerOneWeight = new();

	public Dictionary<string, double> CropsPerOneWeight {
		get => _cropsPerOneWeight;
		set {
			_cropsPerOneWeight = value;
			foreach (var (key, v) in _cropsPerOneWeight) {
				if (key.TryGetCrop(out var crop)) CropWeights[crop] = v;
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
				if (key.TryGetCrop(out var crop)) EventCropWeights[crop] = v;
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


	public Dictionary<Pest, string> PestIds { get; set; } = new();

	[JsonIgnore] private Dictionary<string, int> _pestDropBrackets = new();

	public Dictionary<string, int> PestDropBrackets {
		get => _pestDropBrackets;
		set {
			_pestDropBrackets = value;
			PestDropBracketsList = value.ToList();
		}
	}

	public List<KeyValuePair<string, int>> PestDropBracketsList = [];
	public Dictionary<Pest, PestDropChance> PestCropDropChances { get; set; } = new();

	[JsonIgnore] public Dictionary<int, double>? WeightTargets { get; private set; }

	public Dictionary<int, double> InitializeWeightTargets(ConfigFarmingWeightSettings? weightConfig = null) {
		var targets = new Dictionary<int, double>();
		var weights = weightConfig?.CropWeights ?? FarmingWeightConfig.Settings.CropWeights;

		foreach (var fortune in PestDropBrackets.Values) {
			var chance = PestCropDropChances[Pest.Fly];
			var minimum = chance.GetCropDrops(fortune) / weights[chance.Crop];

			targets[fortune] = minimum;
		}

		WeightTargets = targets;
		return targets;
	}
}

public class PestDropChance
{
	public Crop Crop { get; set; }
	public int Items { get; set; } = 0;
	public int Base { get; set; } = 0;
	public double Scaling { get; set; } = 0;
	public List<PestRngDrop> Rare { get; set; } = [];

	[JsonIgnore] private ConcurrentDictionary<int, double> Precomputed { get; } = new();

	public double GetCropDrops(int fortune) {
		var drops = Base * (fortune / Scaling + Items);
		var rng = Rare.Sum((r) => r.Chance * (fortune / 600f + 1) * r.Drops);
		return drops + rng;
	}

	public double GetCropsToSubtract(int fortune, bool includeZero = false, bool usePrecomputed = true,
		ConfigFarmingWeightSettings? weightConfig = null) {
		if (usePrecomputed && Precomputed.TryGetValue(fortune, out var chance)) return chance;

		var config = weightConfig ?? FarmingWeightConfig.Settings;
		var cropWeights = config.CropWeights;
		var targetWeights = config.WeightTargets ?? config.InitializeWeightTargets(config);

		// Zero fortune means we're ignoring the drops from this bracket
		if (fortune == 0 && !includeZero) {
			if (usePrecomputed) Precomputed.GetOrAdd(fortune, 0);
			return 0;
		}

		var total = GetCropDrops(fortune);
		var divisor = total / cropWeights[Crop];
		var target = targetWeights[fortune];
		var toSubtract = total - total / (divisor) * target;
		
		// Round toSubtract to 5 decimal places to avoid floating point precision issues
		toSubtract = Math.Round(toSubtract, 5);

		if (!usePrecomputed) return toSubtract;

		Precomputed.GetOrAdd(fortune, toSubtract);
		return toSubtract;
	}

	public IReadOnlyDictionary<int, double> GetPrecomputed(ConfigFarmingWeightSettings? weightConfig = null) {
		var pestBrackets = weightConfig?.PestDropBrackets ?? FarmingWeightConfig.Settings.PestDropBrackets;

		if (Precomputed.Count >= pestBrackets.Count) return Precomputed;

		foreach (var fortune in pestBrackets.Values) {
			GetCropsToSubtract(fortune, weightConfig: weightConfig);
		}

		return Precomputed;
	}
}

public class PestRngDrop
{
	public int Drops { get; set; } = 0;
	public double Chance { get; set; } = 0;
}
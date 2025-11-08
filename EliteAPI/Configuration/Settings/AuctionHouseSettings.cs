namespace EliteAPI.Configuration.Settings;

public class AuctionHouseSettings
{
	public int AuctionsRefreshInterval { get; set; }
	public int FullAuctionsRefreshInterval { get; set; }
	public double RecentWindowHours { get; set; } = 2;
	public int ShortTermRepresentativeLowestDays { get; set; } = 3;
	public int LongTermRepresentativeLowestDays { get; set; } = 7;
	public int RecentFallbackMaxLookbackDays { get; set; } = 2;
	public int MinRecentVolumeThreshold { get; set; } = 3;
	public int RecentFallbackTakeCount { get; set; } = 15;
	public int RawDataRetentionDays { get; set; } = 10;
	public int AggregationMaxLookbackDays { get; set; } = 7;
	public List<VariantConfigEntry> Variants { get; set; } = [];
	public List<string> VaryByRarity { get; set; } = [];
	public List<string> VariantOnlySkyblockIds { get; set; } = ["PET", "RUNE", "UNIQUE_RUNE"];
	public Dictionary<string, PetLevelGroupConfig> PetLevelGroups { get; set; } = [];
	public Dictionary<string, Dictionary<string, PetLevelGroupConfig>> PetLevelGroupOverrides { get; set; } = [];
}

public class VariantConfigEntry
{
	public string? SkyblockId { get; set; }
	public string? SkyblockIdPrefix { get; set; }
	public required string Strategy { get; set; }
}

public class PetLevelGroupConfig
{
	public int MinLevel { get; set; }
	public int MaxLevel { get; set; }
	public required string GroupKey { get; set; }
}
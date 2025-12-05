using System.Text.Json.Serialization;

namespace EliteAPI.Features.Recap.Models;

public class YearlyRecapData
{
	public PlayerRecapInfo Player { get; set; } = new();
	public List<ProfileRecapInfo> Profiles { get; set; } = [];
	public AllProfilesSummaryRecap AllProfilesSummary { get; set; } = new();
	public ContestRecap Contests { get; set; } = new();
	public List<EventRecap> Events { get; set; } = [];
	public ShopRecap Shop { get; set; } = new();
	public CollectionRecap Collections { get; set; } = new();
	public PestRecap Pests { get; set; } = new();
	public SkillRecap Skills { get; set; } = new();
	public StreakRecap Streak { get; set; } = new();
	public LeaderboardRecap Leaderboards { get; set; } = new();
	public string Year { get; set; } = string.Empty;
	public string CurrentProfile { get; set; } = string.Empty;
}

public class PlayerRecapInfo
{
	public string Ign { get; set; } = string.Empty;
	public string Uuid { get; set; } = string.Empty;
	public string FirstDataPoint { get; set; } = string.Empty;
	public string LastDataPoint { get; set; } = string.Empty;
	public int DaysActive { get; set; }
	public string MostActiveMonth { get; set; } = string.Empty;
	public FarmingWeightRecap FarmingWeight { get; set; } = new();
}

public class FarmingWeightRecap
{
	public double Gained { get; set; }
	public double Total { get; set; }
	public double AverageComparison { get; set; }
}

public class ProfileRecapInfo
{
	public string Name { get; set; } = string.Empty;
	public string CuteName { get; set; } = string.Empty;
	public bool IsMain { get; set; }
	public bool? Wiped { get; set; }
}

public class AllProfilesSummaryRecap
{
	public double TotalWeightGained { get; set; }
	public double TotalCoinsGained { get; set; }
	public int WipedProfiles { get; set; }
}

public class ContestRecap
{
	public int Total { get; set; }
	public Dictionary<string, int> PerCrop { get; set; } = new();
	public List<ContestPlacementRecap> HighestPlacements { get; set; } = new();
}

public class ContestPlacementRecap
{
	public string Crop { get; set; } = string.Empty;
	public int Rank { get; set; }
	public string Medal { get; set; } = string.Empty;
}

public class EventRecap
{
	public string Name { get; set; } = string.Empty;
	public string Type { get; set; } = string.Empty;
	public bool Participated { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public int? Rank { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public double? Score { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Banner { get; set; }

	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? TypeLabel { get; set; }
}

public class ShopRecap
{
	public bool HasPurchased { get; set; }
}

public class CollectionRecap
{
	public Dictionary<string, long> Increases { get; set; } = new();
	public Dictionary<string, long> GlobalTotals { get; set; } = new();
	public Dictionary<string, double> AverageComparison { get; set; } = new();
	public List<MonthlyStatRecap> Monthly { get; set; } = [];
}

public class MonthlyStatRecap
{
	public string Month { get; set; } = string.Empty;
	public double Amount { get; set; }
}

public class PestRecap
{
	public int Kills { get; set; }
	public Dictionary<string, int> Breakdown { get; set; } = new();
	public long GlobalTotal { get; set; }
	public double AverageComparison { get; set; }
	public List<MonthlyStatRecap> Monthly { get; set; } = [];
}

public class SkillRecap
{
	public double FarmingXp { get; set; }
	public Dictionary<string, double> Breakdown { get; set; } = new();
	public double GlobalTotal { get; set; }
	public double AverageComparison { get; set; }
}

public class LeaderboardRecap
{
	public List<LeaderboardPlacementRecap> Top1000 { get; set; } = [];
	public List<MonthlyLeaderboardRecap> MonthlyHighs { get; set; } = [];
}

public class LeaderboardPlacementRecap
{
	public string Title { get; set; } = string.Empty;
	public string Slug { get; set; } = string.Empty;
	public int Rank { get; set; }
	public double Amount { get; set; }
	public string? ShortTitle { get; set; }
}

public class MonthlyLeaderboardRecap
{
	public string Month { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public int Rank { get; set; }
}

public class GlobalRecap
{
	public long TotalCrops { get; set; }
	public double TotalXp { get; set; }
	public long TotalPests { get; set; }
	public double TotalFarmingWeight { get; set; }
	public int TrackedPlayers { get; set; }
	public int BannedWiped { get; set; }
	public int IronmanToNormal { get; set; }

	public Dictionary<string, long> Crops { get; set; } = new();
	public Dictionary<string, long> TotalCropsBreakdown { get; set; } = new();

	public Dictionary<string, double> Skills { get; set; } = new();
	public Dictionary<string, double> TotalSkillsBreakdown { get; set; } = new();

	public Dictionary<string, long> Pests { get; set; } = new();
	public Dictionary<string, long> TotalPestsBreakdown { get; set; } = new();
}

public class StreakRecap
{
	public int LongestStreakHours { get; set; }
	public long Start { get; set; }
	public long End { get; set; }
	public double AverageDailyDowntime { get; set; }
	public List<long> Sparkline { get; set; } = [];
}
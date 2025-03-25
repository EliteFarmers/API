using System.Text.Json.Serialization;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards;

public interface ILeaderboardDefinition {
	public LeaderboardInfo Info { get; }
}

public class LeaderboardInfo {
	public required string Title { get; set; }
	public string? ShortTitle { get; set; }
	public required string Slug { get; set; }
	public required string Category { get; set; }
	public bool UseIncreaseForInterval { get; set; } = true;
	public List<LeaderboardType> IntervalType { get; set; } = [];
	public LeaderboardScoreDataType ScoreDataType { get; set; }
}

public interface IMemberLeaderboardDefinition : ILeaderboardDefinition 
{
	IConvertible? GetScoreFromMember(ProfileMember member) {
		return null;
	}
}

public interface IProfileLeaderboardDefinition : ILeaderboardDefinition 
{
	IConvertible? GetScoreFromProfile(EliteAPI.Models.Entities.Hypixel.Profile profile) {
		return null;
	}
	
	IConvertible? GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden) {
		return null;
	}
}

public class LeaderboardInfoDto {
	/// <summary>
	/// Leaderboard title
	/// </summary>
	public required string Title { get; set; }
	/// <summary>
	/// Leaderboard short title
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? Short { get; set; }
	/// <summary>
	/// Leaderboard category
	/// </summary>
	public required string Category { get; set; }
	/// <summary>
	/// If true, the leaderboard is profile based
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool Profile { get; set; }
	/// <summary>
	/// Interval type of the leaderboard
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter<LeaderboardType>))]
	public LeaderboardType IntervalType { get; set; }
	/// <summary>
	/// Score data type of the leaderboard
	/// </summary>
	[JsonConverter(typeof(JsonStringEnumConverter<LeaderboardScoreDataType>))]
	public LeaderboardScoreDataType ScoreDataType { get; set; }
}

public static class LeaderboardDefintionExtensions {
	public static bool IsProfileLeaderboard(this ILeaderboardDefinition lb) {
		return lb is IProfileLeaderboardDefinition;
	}
	
	public static bool IsMemberLeaderboard(this ILeaderboardDefinition lb) {
		return lb is IMemberLeaderboardDefinition;
	}
}
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
using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobFirstPlaceContestsLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Jacob Contest First Places",
		ShortTitle = "First Place Contests",
		Slug = "firstplace",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		if (member.JacobData.FirstPlaceScores == 0) return null;
		
		return member.JacobData.FirstPlaceScores;
	}
}
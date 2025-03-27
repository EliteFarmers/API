using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobFirstPlaceContestsLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Jacob Contest First Places",
		ShortTitle = "First Place Contests",
		Slug = "firstplace",
		Category = "General",
		MinimumScore = 10,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.JacobData.FirstPlaceScores;
	}
}
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobMedalsPlatinumLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Platinum Medals Earned",
		ShortTitle = "Platinum Medals",
		Slug = "platinummedals",
		Category = "General",
		MinimumScore = 50,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.JacobData.EarnedMedals.Platinum;
	}
}
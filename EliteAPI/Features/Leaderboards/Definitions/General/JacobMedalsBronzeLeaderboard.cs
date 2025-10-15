using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobMedalsBronzeLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Bronze Medals Earned",
		ShortTitle = "Bronze Medals",
		Slug = "bronzemedals",
		Category = "General",
		MinimumScore = 50,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.JacobData.EarnedMedals.Bronze;
	}
}
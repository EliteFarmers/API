using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobMedalsBronzeLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Bronze Medals Earned",
		ShortTitle = "Bronze Medals",
		Slug = "bronzemedals",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var count = member.JacobData.EarnedMedals.Bronze;
		return (count == 0) ? null : count;
	}
}
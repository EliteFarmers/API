using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobMedalsPlatinumLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Platinum Medals Earned",
		ShortTitle = "Platinum Medals",
		Slug = "platinummedals",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var count = member.JacobData.EarnedMedals.Platinum;
		return (count == 0) ? null : count;
	}
}
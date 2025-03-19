using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class PestRatLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Rat Kills",
		ShortTitle = "Rat",
		Slug = "rat",
		Category = "Pests",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var pest = member.Farming.Pests.Rat;
		
		if (pest == 0) return null;
		return pest;
	}
}

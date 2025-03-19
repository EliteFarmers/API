using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class PestFieldMouseLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Field Mouse Kills",
		ShortTitle = "Field Mouse",
		Slug = "mouse",
		Category = "Pests",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var pest = member.Farming.Pests.Mouse;
		
		if (pest == 0) return null;
		return pest;
	}
}

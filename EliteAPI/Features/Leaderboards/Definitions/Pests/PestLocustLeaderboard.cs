using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class PestLocustLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Locust Kills",
		ShortTitle = "Locust",
		Slug = "locust",
		Category = "Pests",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};
	
	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var pest = member.Farming.Pests.Locust;
		
		if (pest == 0) return null;
		return pest;
	}
}
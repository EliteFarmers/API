using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class VisitorsAcceptedLeaderboard : IProfileLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Visitors Accepted",
		ShortTitle = "Visitors Accepted",
		Slug = "visitors-accepted",
		Category = "General",
		MinimumScore = 500,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		return garden.CompletedVisitors;
	}
}
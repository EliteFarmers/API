using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class VisitorsAcceptedLeaderboard : IProfileLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Visitors Accepted",
		ShortTitle = "Visitors Accepted",
		Slug = "visitors-accepted",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};
	
	public IConvertible? GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden) {
		if (garden.CompletedVisitors == 0) return null;
		return garden.CompletedVisitors;
	}
}
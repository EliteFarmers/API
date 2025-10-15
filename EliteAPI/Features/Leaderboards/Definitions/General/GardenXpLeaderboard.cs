using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class GardenXpLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Garden XP",
		ShortTitle = "Garden XP",
		Slug = "garden",
		Category = "General",
		MinimumScore = 10_000,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		return garden.GardenExperience;
	}
}
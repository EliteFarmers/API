using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class PestLocustLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Locust Kills",
		ShortTitle = "Locust",
		Slug = "locust",
		Category = "Pests",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.Farming.Pests.Locust;
	}
}
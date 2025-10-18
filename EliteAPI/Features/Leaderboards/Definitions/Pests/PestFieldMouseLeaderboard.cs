using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class PestFieldMouseLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Field Mouse Kills",
		ShortTitle = "Field Mouse",
		Slug = "mouse",
		Category = "Pests",
		MinimumScore = 50,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.Farming.Pests.Mouse;
	}
}
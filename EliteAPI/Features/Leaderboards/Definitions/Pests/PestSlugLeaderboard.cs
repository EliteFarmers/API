using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class PestSlugLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Slug Kills",
		ShortTitle = "Slug",
		Slug = "slug",
		Category = "Pests",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};
	
	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.Farming.Pests.Slug;
	}
}
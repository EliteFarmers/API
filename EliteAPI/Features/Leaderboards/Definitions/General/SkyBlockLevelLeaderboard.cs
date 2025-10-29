using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkyblockLevelLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Skyblock Level",
		ShortTitle = "Skyblock Level",
		Slug = "skyblockxp",
		Category = "General",
		MinimumScore = 5_000, // Skyblock level 50
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "GOLDEN_FRAGMENT"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.SkyblockXp;
	}
}
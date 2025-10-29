using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CatacombsLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Catacombs Experience",
		ShortTitle = "Catacombs XP",
		Slug = "catacombs",
		Category = "Dungeons",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "WITHER_RELIC"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)(member.Unparsed.Dungeons.DungeonTypes?.Catacombs?.Experience ?? 0);
	}
}
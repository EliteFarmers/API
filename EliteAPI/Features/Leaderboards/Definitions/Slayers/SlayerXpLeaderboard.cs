using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class TotalSlayerXpLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Total Slayer Experience",
		ShortTitle = "Slayer XP",
		Slug = "slayer-xp",
		Category = "Slayers",
		MinimumScore = 1_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "AATROX_BATPHONE",
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Xp is null) return 0;
		return (decimal)member.Slayers.Xp;
	}
}
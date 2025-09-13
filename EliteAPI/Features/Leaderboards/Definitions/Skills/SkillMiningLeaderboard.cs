using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillMiningLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mining Experience",
		ShortTitle = "Mining XP",
		Slug = "mining",
		Category = "Skills",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal) member.Skills.Mining;
	}
}

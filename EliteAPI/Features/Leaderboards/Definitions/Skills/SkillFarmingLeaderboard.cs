using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillFarmingLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Farming Experience",
		ShortTitle = "Farming",
		Slug = "farming",
		Category = "Skills",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal) member.Skills.Farming;
	}
}
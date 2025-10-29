using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillCombatLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Combat Experience",
		ShortTitle = "Combat XP",
		Slug = "combat",
		Category = "Skills",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "IRON_SWORD"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)member.Skills.Combat;
	}
}
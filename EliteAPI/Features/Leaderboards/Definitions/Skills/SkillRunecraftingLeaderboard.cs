using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillRunecraftingLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Runecrafting Experience",
		ShortTitle = "Runecrafting XP",
		Slug = "runecrafting",
		Category = "Skills",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "RUNE_RAINBOW_3"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)member.Skills.Runecrafting;
	}
}
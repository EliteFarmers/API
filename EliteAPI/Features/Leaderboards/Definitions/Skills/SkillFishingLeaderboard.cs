using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillFishingLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Fishing Experience",
		ShortTitle = "Fishing XP",
		Slug = "fishing",
		Category = "Skills",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "FISHING_ROD"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)member.Skills.Fishing;
	}
}
using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillFishingLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Fishing Experience",
		ShortTitle = "Fishing",
		Slug = "fishing",
		Category = "Skills",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var skill = member.Skills.Fishing;
		
		if (skill == 0) return null;
		return skill;
	}
}

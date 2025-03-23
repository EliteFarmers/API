using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillRunecraftingLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Runecrafting Experience",
		ShortTitle = "Runecrafting",
		Slug = "runecrafting",
		Category = "Skills",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var skill = member.Skills.Runecrafting;
		
		if (skill == 0) return null;
		return skill;
	}
}

using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillAlchemyLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Alchemy Experience",
		ShortTitle = "Alchemy",
		Slug = "alchemy",
		Category = "Skills",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var skill = member.Skills.Alchemy;
		
		if (skill == 0) return null;
		return skill;
	}
}
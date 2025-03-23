using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillCarpentryLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Carpentry Experience",
		ShortTitle = "Carpentry",
		Slug = "carpentry",
		Category = "Skills",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var skill = member.Skills.Carpentry;
		
		if (skill == 0) return null;
		return skill;
	}
}
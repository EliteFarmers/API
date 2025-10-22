using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkillSocialLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Social Experience",
		ShortTitle = "Social XP",
		Slug = "social",
		Category = "Skills",
		MinimumScore = 10_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)member.Skills.Social;
	}
}

public class SkillSocialProfileLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Co-op Social Experience",
		ShortTitle = "Co-op Social XP",
		Slug = "coop-social",
		Category = "Skills",
		MinimumScore = 15_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public decimal GetScoreFromProfile(Profile profile, LeaderboardType type) {
		return (decimal)profile.SocialXp;
	}
}
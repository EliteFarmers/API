using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class SkyblockLevelLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Skyblock Level",
		ShortTitle = "Skyblock Level",
		Slug = "skyblockxp",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		if (member.SkyblockXp == 0) return null;
		
		return member.SkyblockXp;
	}
}
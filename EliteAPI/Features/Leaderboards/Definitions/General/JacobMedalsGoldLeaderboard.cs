using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class JacobMedalsGoldLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Gold Medals Earned",
		ShortTitle = "Gold Medals",
		Slug = "goldmedals",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var count = member.JacobData.EarnedMedals.Gold;
		return (count == 0) ? null : count;
	}
}
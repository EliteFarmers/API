using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class TotalChocolateLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "All-Time Chocolate",
		ShortTitle = "Chocolate",
		Slug = "chocolate",
		Category = "General",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		if (member.ChocolateFactory.TotalChocolate == 0) return null;
		
		return member.ChocolateFactory.TotalChocolate;
	}
}
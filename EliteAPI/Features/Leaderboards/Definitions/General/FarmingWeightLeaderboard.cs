using EliteAPI.Features.Leaderboards.Models;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class FarmingWeightLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Farming Weight",
		ShortTitle = "Farming Weight",
		Slug = "farmingweight",
		Category = "General",
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};
	
	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		if (member.Farming.TotalWeight is 0 or < 0.001) return null;
		
		return member.Farming.TotalWeight;
	}
}
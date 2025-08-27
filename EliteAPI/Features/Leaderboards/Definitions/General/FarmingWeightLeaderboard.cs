using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class FarmingWeightLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Farming Weight",
		ShortTitle = "Farming Weight",
		Slug = "farmingweight",
		Category = "General",
		MinimumScore = 500,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Decimal
	};
	
	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type == LeaderboardType.Current) {
			return (decimal) member.Farming.TotalWeight;
		}
		
		// Don't allow initial score to be set while API toggle is off
		if (!member.Api.Collections) return 0;
		
		var cropWeight = member.Farming.CropWeight.Values.Sum(c => c);
		return (decimal) cropWeight;
	}
}
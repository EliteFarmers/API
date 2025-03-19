using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropNetherWartLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Nether Wart Collection",
		ShortTitle = "Nether Wart",
		Slug = "netherwart",
		Category = "Crops",
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var crop = member.Collections.RootElement.TryGetProperty(CropId.NetherWart, out var value) 
			? value.GetInt64() 
			: 0;
		
		if (crop == 0) return null;
		return crop;
	}
}
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropPotatoLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Potato Collection",
		ShortTitle = "Potato",
		Slug = "potato",
		Category = "Crops",
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var crop = member.Collections.RootElement.TryGetProperty(CropId.Potato, out var value) 
			? value.GetInt64() 
			: 0;
		
		if (crop == 0) return null;
		return crop;
	}
}
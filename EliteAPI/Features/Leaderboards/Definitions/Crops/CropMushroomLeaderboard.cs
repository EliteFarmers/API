using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropMushroomLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mushroom Collection",
		ShortTitle = "Mushroom",
		Slug = "mushroom",
		Category = "Crops",
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};
	
	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var crop = member.Collections.RootElement.TryGetProperty(CropId.Mushroom, out var value) 
			? value.GetInt64() 
			: 0;
		
		if (crop == 0) return null;
		return crop;
	}
}

public class MilestoneMushroomLeaderboard : IProfileLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mushroom Milestone Collection",
		ShortTitle = "Mushroom Milestone",
		Slug = "mushroom-milestone",
		Category = "Milstones",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden) {
		var crop = garden.Crops.Mushroom;
		
		if (crop == 0) return null;
		return crop;
	}
}
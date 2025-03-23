using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropCocoaLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Cocoa Bean Collection",
		ShortTitle = "Cocoa Bean",
		Slug = "cocoa",
		Category = "Crops",
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromMember(EliteAPI.Models.Entities.Hypixel.ProfileMember member) {
		var crop = member.Collections.RootElement.TryGetProperty(CropId.CocoaBeans, out var value) 
			? value.GetInt64() 
			: 0;
		
		if (crop == 0) return null;
		return crop;
	}
}

public class MilestoneCocoaLeaderboard : IProfileLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Cocoa Bean Milestone Collection",
		ShortTitle = "Cocoa Bean Milestone",
		Slug = "cocoa-milestone",
		Category = "Milstones",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden) {
		var crop = garden.Crops.CocoaBeans;
		
		if (crop == 0) return null;
		return crop;
	}
}
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

public class MilestoneNetherWartLeaderboard : IProfileLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Nether Wart Milestone Collection",
		ShortTitle = "Nether Wart Milestone",
		Slug = "netherwart-milestone",
		Category = "Milstones",
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	public IConvertible? GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden) {
		var crop = garden.Crops.NetherWart;
		
		if (crop == 0) return null;
		return crop;
	}
}
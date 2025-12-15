using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropSunflowerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sunflower Collection",
		ShortTitle = "Sunflower",
		Slug = "sunflower",
		Category = "Crops",
		Source = LeaderboardSourceType.Collection,
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Sunflower,
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;
		var crop = member.Collections.GetValueOrDefault(CropId.Sunflower, 0);

		return crop;
	}
}

public class MilestoneSunflowerLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sunflower Milestone Collection",
		ShortTitle = "Sunflower Milestone",
		Slug = "sunflower-milestone",
		Category = "Milestones",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Sunflower,
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		var crop = garden.Crops.Sunflower;

		return crop;
	}
}
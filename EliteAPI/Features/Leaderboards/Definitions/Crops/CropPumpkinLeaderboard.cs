using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropPumpkinLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Pumpkin Collection",
		ShortTitle = "Pumpkin",
		Slug = "pumpkin",
		Category = "Crops",
		Source = LeaderboardSourceType.Collection,
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Pumpkin,
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;
		var crop = member.Collections.GetValueOrDefault(CropId.Pumpkin, 0);

		return crop;
	}
}

public class MilestonePumpkinLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Pumpkin Milestone Collection",
		ShortTitle = "Pumpkin Milestone",
		Slug = "pumpkin-milestone",
		Category = "Milestones",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Pumpkin,
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		var crop = garden.Crops.Pumpkin;

		return crop;
	}
}
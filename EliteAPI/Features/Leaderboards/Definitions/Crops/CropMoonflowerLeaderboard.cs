using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropMoonflowerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Moonflower Collection",
		ShortTitle = "Moonflower",
		Slug = "moonflower",
		Category = "Crops",
		Source = LeaderboardSourceType.Collection,
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Moonflower,
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;
		var crop = member.Collections.GetValueOrDefault(CropId.Moonflower, 0);

		return crop;
	}
}

public class MilestoneMoonflowerLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Moonflower Milestone Collection",
		ShortTitle = "Moonflower Milestone",
		Slug = "moonflower-milestone",
		Category = "Milestones",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Moonflower,
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		var crop = garden.Crops.Moonflower;

		return crop;
	}
}
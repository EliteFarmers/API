using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropSugarCaneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sugar Cane Collection",
		ShortTitle = "Sugar Cane",
		Slug = "sugarcane",
		Category = "Crops",
		Source = LeaderboardSourceType.Collection,
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.SugarCane,
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;
		var crop = member.Collections.GetValueOrDefault(CropId.SugarCane, 0);

		return crop;
	}
}

public class MilestoneSugarCaneLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sugar Cane Milestone Collection",
		ShortTitle = "Sugar Cane Milestone",
		Slug = "sugarcane-milestone",
		Category = "Milestones",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.SugarCane,
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		var crop = garden.Crops.SugarCane;

		return crop;
	}
}
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Farming;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class CropPotatoLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Potato Collection",
		ShortTitle = "Potato",
		Slug = "potato",
		Category = "Crops",
		Source = LeaderboardSourceType.Collection,
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Potato,
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;
		var crop = member.Collections.GetValueOrDefault(CropId.Potato, 0);

		return crop;
	}
}

public class MilestonePotatoLeaderboard : IProfileLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Potato Milestone Collection",
		ShortTitle = "Potato Milestone",
		Slug = "potato-milestone",
		Category = "Milestones",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CropId.Potato,
	};

	public decimal GetScoreFromGarden(EliteAPI.Models.Entities.Hypixel.Garden garden, LeaderboardType type) {
		var crop = garden.Crops.Potato;

		return crop;
	}
}
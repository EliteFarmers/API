using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class TotalPestKillsLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Pest Kills",
		ShortTitle = "Pest Kills",
		Slug = "pests",
		Category = "Pests",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current, LeaderboardType.Monthly],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = "PESTHUNTER_ARTIFACT"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return member.Farming.Pests.Beetle
		       + member.Farming.Pests.Cricket
		       + member.Farming.Pests.Fly
		       + member.Farming.Pests.Locust
		       + member.Farming.Pests.Mite
		       + member.Farming.Pests.Mosquito
		       + member.Farming.Pests.Moth
		       + member.Farming.Pests.Mouse
		       + member.Farming.Pests.Rat
		       + member.Farming.Pests.Slug
		       + member.Farming.Pests.Earthworm
		       + member.Farming.Pests.Dragonfly
		       + member.Farming.Pests.Firefly
		       + member.Farming.Pests.Mantis;
	}
}
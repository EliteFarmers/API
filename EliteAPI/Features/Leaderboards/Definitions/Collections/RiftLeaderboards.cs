using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class AgaricusCapLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Agaricus Cap Collection",
		ShortTitle = "Agaricus Cap",
		Slug = "agaricus-cap",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "AGARICUS_CAP";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class CaducousStemLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Caducous Stem Collection",
		ShortTitle = "Caducous Stem",
		Slug = "caducous-stem",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "CADUCOUS_STEM";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class HalfEatenCarrotLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Half-Eaten Carrot Collection",
		ShortTitle = "Half-Eaten Carrot",
		Slug = "half-eaten-carrot",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "HALF_EATEN_CARROT";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class HemovibeLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Hemovibe Collection",
		ShortTitle = "Hemovibe",
		Slug = "hemovibe",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "HEMOVIBE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class LivingMetalHeartLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Living Metal Heart Collection",
		ShortTitle = "Living Metal Heart",
		Slug = "living-metal-heart",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "METAL_HEART";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class WiltedBerberisLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Wilted Berberis Collection",
		ShortTitle = "Wilted Berberis",
		Slug = "wilted-berberis",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "WILTED_BERBERIS";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class TimiteLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Timite Collection",
		ShortTitle = "Timite",
		Slug = "timite",
		Category = "Rift",
		MinimumScore = 100,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "TIMITE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}
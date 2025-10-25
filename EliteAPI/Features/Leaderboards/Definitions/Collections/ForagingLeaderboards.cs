using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class AcaciaLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Acacia Wood Collection",
		ShortTitle = "Acacia",
		Slug = "acacia",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LOG_2";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class BirchLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Birch Wood Collection",
		ShortTitle = "Birch",
		Slug = "birch",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LOG:2";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class DarkOakLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Dark Oak Wood Collection",
		ShortTitle = "Dark Oak",
		Slug = "dark-oak",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LOG_2:1";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class JungleLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Jungle Wood Collection",
		ShortTitle = "Jungle",
		Slug = "jungle",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LOG:3";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class OakLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Oak Wood Collection",
		ShortTitle = "Oak",
		Slug = "oak",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LOG";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SpruceLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Spruce Wood Collection",
		ShortTitle = "Spruce",
		Slug = "spruce",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LOG:1";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SeaLumiesLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sea Lumies Collection",
		ShortTitle = "Sea Lumies",
		Slug = "sea-lumies",
		Category = "Foraging",
		MinimumScore = 5_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SEA_LUMIES";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class VinesapLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Vinesap Collection",
		ShortTitle = "Vinesap",
		Slug = "vinesap",
		Category = "Foraging",
		MinimumScore = 1_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "VINESAP";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class LushlilacLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Lushlilac Collection",
		ShortTitle = "Lushlilac",
		Slug = "lushlilac",
		Category = "Foraging",
		MinimumScore = 500,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "LUSHLILAC";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class MangroveLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mangrove Wood Collection",
		ShortTitle = "Mangrove",
		Slug = "mangrove",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "MANGROVE_LOG";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class FigLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Fig Wood Collection",
		ShortTitle = "Fig",
		Slug = "fig",
		Category = "Foraging",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "FIG_LOG";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class TenderWoodLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Tender Wood Collection",
		ShortTitle = "Tender Wood",
		Slug = "tender-wood",
		Category = "Foraging",
		MinimumScore = 1_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "TENDER_WOOD";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class BlazeRodLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Blaze Rod Collection",
		ShortTitle = "Blaze Rod",
		Slug = "blaze-rod",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "BLAZE_ROD";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class BoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Bone Collection",
		ShortTitle = "Bone",
		Slug = "bone",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "BONE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class ChiliPepperLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Chili Pepper Collection",
		ShortTitle = "Chili Pepper",
		Slug = "chili-pepper",
		Category = "Combat",
		MinimumScore = 10,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "CHILI_PEPPER";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class EnderPearlLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Ender Pearl Collection",
		ShortTitle = "Ender Pearl",
		Slug = "ender-pearl",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "ENDER_PEARL";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GhastTearLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Ghast Tear Collection",
		ShortTitle = "Ghast Tear",
		Slug = "ghast-tear",
		Category = "Combat",
		MinimumScore = 10_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "GHAST_TEAR";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GunpowderLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Gunpowder Collection",
		ShortTitle = "Gunpowder",
		Slug = "gunpowder",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SULPHUR";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class MagmaCreamLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Magma Cream Collection",
		ShortTitle = "Magma Cream",
		Slug = "magma-cream",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "MAGMA_CREAM";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class RottenFleshLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Rotten Flesh Collection",
		ShortTitle = "Rotten Flesh",
		Slug = "rotten-flesh",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "ROTTEN_FLESH";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SlimeballLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Slimeball Collection",
		ShortTitle = "Slimeball",
		Slug = "slimeball",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SLIME_BALL";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SpiderEyeLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Spider Eye Collection",
		ShortTitle = "Spider Eye",
		Slug = "spider-eye",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SPIDER_EYE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class StringLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "String Collection",
		ShortTitle = "String",
		Slug = "string",
		Category = "Combat",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "STRING";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}
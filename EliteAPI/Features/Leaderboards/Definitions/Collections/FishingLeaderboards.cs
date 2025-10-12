using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class ClayLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Clay Collection",
		ShortTitle = "Clay",
		Slug = "clay",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "CLAY_BALL";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class ClownfishLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Clownfish Collection",
		ShortTitle = "Clownfish",
		Slug = "clownfish",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "RAW_FISH:2";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class InkSacLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Ink Sac Collection",
		ShortTitle = "Ink Sac",
		Slug = "ink-sac",
		Category = "Fishing",
		MinimumScore = 10_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "INK_SACK";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class LilyPadLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Lily Pad Collection",
		ShortTitle = "Lily Pad",
		Slug = "lily-pad",
		Category = "Fishing",
		MinimumScore = 50_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "WATER_LILY";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class MagmafishLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Magmafish Collection",
		ShortTitle = "Magmafish",
		Slug = "magmafish",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "MAGMA_FISH";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class PrismarineCrystalsLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Prismarine Crystals Collection",
		ShortTitle = "Prismarine Crystals",
		Slug = "prismarine-crystals",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "PRISMARINE_CRYSTALS";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class PrismarineShardLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Prismarine Shard Collection",
		ShortTitle = "Prismarine Shard",
		Slug = "prismarine-shard",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "PRISMARINE_SHARD";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class PufferfishLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Pufferfish Collection",
		ShortTitle = "Pufferfish",
		Slug = "pufferfish",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "RAW_FISH:3";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class RawFishLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Raw Fish Collection",
		ShortTitle = "Raw Fish",
		Slug = "raw-fish",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "RAW_FISH";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class RawSalmonLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Raw Salmon Collection",
		ShortTitle = "Raw Salmon",
		Slug = "raw-salmon",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "RAW_FISH:1";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SpongeLeaderboard : IMemberLeaderboardDefinition {
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sponge Collection",
		ShortTitle = "Sponge",
		Slug = "sponge",
		Category = "Fishing",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long
	};

	private const string CollectionId = "SPONGE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}
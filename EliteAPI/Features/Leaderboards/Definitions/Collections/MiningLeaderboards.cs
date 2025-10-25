using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Parsers.Profiles;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class LapisLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Lapis Lazuli Collection",
		ShortTitle = "Lapis",
		Slug = "lapis",
		Category = "Mining",
		MinimumScore = 500_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "INK_SACK:4";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class RedstoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Redstone Collection",
		ShortTitle = "Redstone",
		Slug = "redstone",
		Category = "Mining",
		MinimumScore = 500_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "REDSTONE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class EmeraldLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Emerald Collection",
		ShortTitle = "Emerald",
		Slug = "emerald",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "EMERALD";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class DiamondLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Diamond Collection",
		ShortTitle = "Diamond",
		Slug = "diamond",
		Category = "Mining",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "DIAMOND";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class CoalLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Coal Collection",
		ShortTitle = "Coal",
		Slug = "coal",
		Category = "Mining",
		MinimumScore = 500_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "COAL";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class NetherQuartzLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Nether Quartz Collection",
		ShortTitle = "Nether Quartz",
		Slug = "nether-quartz",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "QUARTZ";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GoldLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Gold Ingot Collection",
		ShortTitle = "Gold Ingot",
		Slug = "gold",
		Category = "Mining",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "GOLD_INGOT";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class IronLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Iron Ingot Collection",
		ShortTitle = "Iron Ingot",
		Slug = "iron",
		Category = "Mining",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "IRON_INGOT";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SandLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sand Collection",
		ShortTitle = "Sand",
		Slug = "sand",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SAND";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class RedSandLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Red Sand Collection",
		ShortTitle = "Red Sand",
		Slug = "red-sand",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SAND:1";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class CobblestoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Cobblestone Collection",
		ShortTitle = "Cobblestone",
		Slug = "cobblestone",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "COBBLESTONE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class ObsidianLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Obsidian Collection",
		ShortTitle = "Obsidian",
		Slug = "obsidian",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "OBSIDIAN";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class EndStoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "End Stone Collection",
		ShortTitle = "End Stone",
		Slug = "end-stone",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "ENDER_STONE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GlowstoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Glowstone Collection",
		ShortTitle = "Glowstone",
		Slug = "glowstone",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "GLOWSTONE_DUST";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GravelLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Gravel Collection",
		ShortTitle = "Gravel",
		Slug = "gravel",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "GRAVEL";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class NetherrackLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Netherrack Collection",
		ShortTitle = "Netherrack",
		Slug = "netherrack",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "NETHERRACK";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class IceLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Ice Collection",
		ShortTitle = "Ice",
		Slug = "ice",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "ICE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class MyceliumLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mycelium Collection",
		ShortTitle = "Mycelium",
		Slug = "mycelium",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "MYCEL";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GemstoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Gemstone Collection",
		ShortTitle = "Gemstone",
		Slug = "gemstone",
		Category = "Mining",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "GEMSTONE_COLLECTION";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class MithrilLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mithril Collection",
		ShortTitle = "Mithril",
		Slug = "mithril",
		Category = "Mining",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "MITHRIL_ORE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class HardStoneLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Hard Stone Collection",
		ShortTitle = "Hard Stone",
		Slug = "hard-stone",
		Category = "Mining",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "HARD_STONE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class TungstenLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Tungsten Collection",
		ShortTitle = "Tungsten",
		Slug = "tungsten",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "TUNGSTEN";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class UmberLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Umber Collection",
		ShortTitle = "Umber",
		Slug = "umber",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "UMBER";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class GlaciteLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Glacite Collection",
		ShortTitle = "Glacite",
		Slug = "glacite",
		Category = "Mining",
		MinimumScore = 100_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "GLACITE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}

public class SulphurLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Sulphur Collection",
		ShortTitle = "Sulphur",
		Slug = "sulphur",
		Category = "Mining",
		MinimumScore = 50_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Long,
		ItemId = CollectionId
	};

	private const string CollectionId = "SULPHUR_ORE";

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (type != LeaderboardType.Current && !member.Api.Collections) return 0;

		member.TryGetCollection(CollectionId, out var collection);
		return collection;
	}
}
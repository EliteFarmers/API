using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class ArcherLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Archer Experience",
		ShortTitle = "Archer XP",
		Slug = "archer-xp",
		Category = "Dungeons",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "BOW"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)(member.Unparsed.Dungeons.PlayerClasses?.Archer?.Experience ?? 0);
	}
}

public class BerserkLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Berserk Experience",
		ShortTitle = "Berserk XP",
		Slug = "berserk-xp",
		Category = "Dungeons",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "IRON_SWORD"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)(member.Unparsed.Dungeons.PlayerClasses?.Berserk?.Experience ?? 0);
	}
}

public class HealerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Healer Experience",
		ShortTitle = "Healer XP",
		Slug = "healer-xp",
		Category = "Dungeons",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "HEALING_RING"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)(member.Unparsed.Dungeons.PlayerClasses?.Healer?.Experience ?? 0);
	}
}

public class MageLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Mage Experience",
		ShortTitle = "Mage XP",
		Slug = "mage-xp",
		Category = "Dungeons",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "BLAZE_ROD"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)(member.Unparsed.Dungeons.PlayerClasses?.Mage?.Experience ?? 0);
	}
}

public class TankLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Tank Experience",
		ShortTitle = "Tank XP",
		Slug = "tank-xp",
		Category = "Dungeons",
		MinimumScore = 1_000_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "ZOMBIE_COMMANDER_CHESTPLATE"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		return (decimal)(member.Unparsed.Dungeons.PlayerClasses?.Tank?.Experience ?? 0);
	}
}
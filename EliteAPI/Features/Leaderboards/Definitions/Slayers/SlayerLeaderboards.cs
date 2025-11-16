using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class ZombieSlayerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Zombie Slayer Experience",
		ShortTitle = "Zombie Slayer XP",
		Slug = "zombie-slayer",
		Category = "Slayers",
		MinimumScore = 1_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "ROTTEN_FLESH",
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Bosses?.Zombie?.Xp is null) return 0;
		return (decimal)(member.Slayers.Bosses.Zombie.Xp);
	}
}

public class SpiderSlayerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Spider Slayer Experience",
		ShortTitle = "Spider Slayer XP",
		Slug = "spider-slayer",
		Category = "Slayers",
		MinimumScore = 1_000,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "WEB",
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Bosses?.Spider?.Xp is null) return 0;
		return (decimal)(member.Slayers.Bosses.Spider.Xp);
	}
}

public class WolfSlayerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Wolf Slayer Experience",
		ShortTitle = "Wolf Slayer XP",
		Slug = "wolf-slayer",
		Category = "Slayers",
		MinimumScore = 1_500,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "BONE",
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Bosses?.Wolf?.Xp is null) return 0;
		return (decimal)(member.Slayers.Bosses.Wolf.Xp);
	}
}

public class EndermanSlayerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Enderman Slayer Experience",
		ShortTitle = "Enderman Slayer XP",
		Slug = "enderman-slayer",
		Category = "Slayers",
		MinimumScore = 1_500,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "ENDER_PEARL",
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Bosses?.Enderman?.Xp is null) return 0;
		return (decimal)(member.Slayers.Bosses.Enderman.Xp);
	}
}


public class BlazeSlayerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Blaze Slayer Experience",
		ShortTitle = "Blaze Slayer XP",
		Slug = "blaze-slayer",
		Category = "Slayers",
		MinimumScore = 1_500,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "BLAZE_POWDER"
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Bosses?.Blaze?.Xp is null) return 0;
		return (decimal)(member.Slayers.Bosses.Blaze.Xp);
	}
}

public class VampireSlayerLeaderboard : IMemberLeaderboardDefinition
{
	public LeaderboardInfo Info { get; } = new() {
		Title = "Vampire Slayer Experience",
		ShortTitle = "Vampire Slayer XP",
		Slug = "vampire-slayer",
		Category = "Slayers",
		MinimumScore = 75,
		IntervalType = [LeaderboardType.Current],
		ScoreDataType = LeaderboardScoreDataType.Decimal,
		ItemId = "REDSTONE",
	};

	public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
		if (member.Slayers?.Bosses?.Vampire?.Xp is null) return 0;
		return (decimal)(member.Slayers.Bosses.Vampire.Xp);
	}
}

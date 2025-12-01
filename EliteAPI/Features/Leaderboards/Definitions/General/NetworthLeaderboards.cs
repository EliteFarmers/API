using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class NormalNetworthLeaderboard : IMemberLeaderboardDefinition
{
    public LeaderboardInfo Info { get; } = new() {
        Title = "Normal Networth",
        ShortTitle = "Normal Networth",
        Slug = "networth-normal",
        Category = "General",
        MinimumScore = 1_000_000_000,
        IntervalType = [LeaderboardType.Current],
        ScoreDataType = LeaderboardScoreDataType.Decimal
    };

    public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
        if (type != LeaderboardType.Current) return 0;
        return (decimal)member.Networth;
    }
}

public class LiquidNetworthLeaderboard : IMemberLeaderboardDefinition
{
    public LeaderboardInfo Info { get; } = new() {
        Title = "Liquid Networth",
        ShortTitle = "Liquid Networth",
        Slug = "networth-liquid",
        Category = "General",
        MinimumScore = 1_000_000_000,
        IntervalType = [LeaderboardType.Current],
        ScoreDataType = LeaderboardScoreDataType.Decimal
    };

    public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
        if (type != LeaderboardType.Current) return 0;
        return (decimal)member.LiquidNetworth;
    }
}

public class FunctionalNetworthLeaderboard : IMemberLeaderboardDefinition
{
    public LeaderboardInfo Info { get; } = new() {
        Title = "Non-Cosmetic Networth",
        ShortTitle = "Non-Cosmetic Networth",
        Slug = "networth-functional",
        Category = "General",
        MinimumScore = 1_000_000_000,
        IntervalType = [LeaderboardType.Current],
        ScoreDataType = LeaderboardScoreDataType.Decimal
    };

    public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
        if (type != LeaderboardType.Current) return 0;
        return (decimal)member.FunctionalNetworth;
    }
}

public class FunctionalLiquidNetworthLeaderboard : IMemberLeaderboardDefinition
{
    public LeaderboardInfo Info { get; } = new() {
        Title = "Non-Cosmetic Liquid Networth",
        ShortTitle = "NC Liquid Networth",
        Slug = "networth-functional-liquid",
        Category = "General",
        MinimumScore = 1_000_000_000,
        IntervalType = [LeaderboardType.Current],
        ScoreDataType = LeaderboardScoreDataType.Decimal
    };

    public decimal GetScoreFromMember(ProfileMember member, LeaderboardType type) {
        if (type != LeaderboardType.Current) return 0;
        return (decimal)member.LiquidFunctionalNetworth;
    }
}
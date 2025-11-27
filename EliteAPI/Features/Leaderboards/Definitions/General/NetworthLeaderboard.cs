using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Features.Leaderboards.Definitions;

public class NetworthLeaderboard : IMemberLeaderboardDefinition
{
    public LeaderboardInfo Info { get; } = new() {
        Title = "Networth",
        ShortTitle = "Networth",
        Slug = "networth",
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

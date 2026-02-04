using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Features.Leaderboards.Services;

namespace EliteAPI.Features.Leaderboards.Models;

public class LeaderboardRankRequestWithoutId
{
    public string? PlayerUuid { get; set; }
    public string? ProfileId { get; set; }
    public string? ResourceId { get; set; }
    public int? Upcoming { get; set; }
    public int? Previous { get; set; }
    public int? AtRank { get; set; }
    public double? AtAmount { get; set; }
    public string? GameMode { get; set; }
    public RemovedFilter RemovedFilter { get; set; } = RemovedFilter.NotRemoved;
    public string? Identifier { get; set; }
    public bool SkipUpdate { get; set; }
    public CancellationToken? CancellationToken { get; set; }
}

public class LeaderboardRankRequest : LeaderboardRankRequestWithoutId
{
    public required string LeaderboardId { get; set; }
}

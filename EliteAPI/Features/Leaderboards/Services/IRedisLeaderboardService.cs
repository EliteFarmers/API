using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Features.Leaderboards.Services;

public interface IRedisLeaderboardService
{
    Task<LeaderboardPositionDto> GetLeaderboardRank(LeaderboardRankRequest request);

    Task<Dictionary<string, LeaderboardPositionDto?>> GetMultipleLeaderboardRanks(
        List<string> leaderboards, LeaderboardRankRequestWithoutId request);
        
    double GetLeaderboardMinScore(string leaderboardId);
    Task<double> GetCachedMinScore(string leaderboardId, string gameMode = "all");
}

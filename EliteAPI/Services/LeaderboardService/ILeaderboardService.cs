using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Services.LeaderboardService; 

public interface ILeaderboardService {
    public Task<List<LeaderboardEntry>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntryWithRank>> GetLeaderboardSliceAtScore(string leaderboardId, double score, int limit = 20, string? excludeMemberId = null);
    public Task<List<LeaderboardEntry>> GetSkillLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntry>> GetCollectionLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<LeaderboardPositionsDto> GetLeaderboardPositions(string memberId);
    public Task<int> GetLeaderboardPosition(string leaderboardId, string memberId);
    public Task<LeaderboardEntryWithRank?> GetLeaderboardEntry(string leaderboardId, string memberId);
    public bool TryGetLeaderboardSettings(string leaderboardId, out Leaderboard? settings);
    public void UpdateLeaderboardScore(string leaderboardId, string memberId, double score);
}
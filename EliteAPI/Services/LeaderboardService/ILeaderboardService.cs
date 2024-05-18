using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Services.LeaderboardService; 

public interface ILeaderboardService {
    public Task<List<LeaderboardEntry>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntryWithRank>> GetLeaderboardSliceAtScore(string leaderboardId, double score, int limit = 20, string? excludeMemberId = null);
    
    public Task<LeaderboardPositionsDto> GetLeaderboardPositions(string memberId);
    public Task<(int rank, double score)> GetLeaderboardPositionAndScore(string leaderboardId, string memberId, bool includeScore = true);
    public void UpdateLeaderboardScore(string leaderboardId, string memberId, double score);
    public Task RemoveMemberFromAllLeaderboards(string memberId);
    public Task RemoveMemberFromLeaderboards(IEnumerable<string> leaderboardIds, string memberId);
    public Task FetchLeaderboard(string leaderboardId);
    
    public bool TryGetLeaderboardSettings(string leaderboardId, out Leaderboard? settings);

}
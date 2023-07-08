using EliteAPI.Config.Settings;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Services.LeaderboardService; 

public interface ILeaderboardService {
    public Task<List<LeaderboardEntry>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntry>> GetSkillLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntry>> GetCollectionLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<LeaderboardPositionsDto> GetLeaderboardPositions(string memberId);
    public Task<int> GetLeaderboardPosition(string leaderboardId, string memberId);
    public bool TryGetLeaderboardSettings(string leaderboardId, out Leaderboard? settings);
}
namespace EliteAPI.Services.LeaderboardService; 

public interface ILeaderboardService {
    public Task<List<LeaderboardEntry>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntry>> GetSkillLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
    public Task<List<LeaderboardEntry>> GetCollectionLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
}
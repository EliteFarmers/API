namespace EliteAPI.Services.LeaderboardService; 

public interface ILeaderboardService {
    public Task<List<LeaderboardEntry<double>>> GetLeaderboardSlice(string leaderboardId, int offset = 0, int limit = 20);
}
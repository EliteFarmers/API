using System.Diagnostics.CodeAnalysis;

namespace EliteAPI.Configuration.Settings; 

public class ConfigLeaderboardSettings {
    public int CompleteRefreshInterval { get; set; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, Leaderboard> Leaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> CollectionLeaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> SkillLeaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> PestLeaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> ProfileLeaderboards { get; set; } = new();

    public Leaderboard? GetLeaderboardSettings(string leaderboardId, bool includeProfile = true) {
        if (CollectionLeaderboards.TryGetValue(leaderboardId, out var lb)
            || SkillLeaderboards.TryGetValue(leaderboardId, out lb)
            || Leaderboards.TryGetValue(leaderboardId, out lb)
            || PestLeaderboards.TryGetValue(leaderboardId, out lb)
            || (includeProfile && ProfileLeaderboards.TryGetValue(leaderboardId, out lb))) {
            return lb;
        }

        return null;
    }

    public bool TryGetLeaderboardSettings(string leaderboardId, [NotNullWhen(true)] out Leaderboard? settings, bool includeProfile = true) {
        settings = GetLeaderboardSettings(leaderboardId, includeProfile);
        return settings != null;
    }
    
    public bool HasLeaderboard(string leaderboardId) {
        return CollectionLeaderboards.ContainsKey(leaderboardId)
               || SkillLeaderboards.ContainsKey(leaderboardId)
               || Leaderboards.ContainsKey(leaderboardId)
               || PestLeaderboards.ContainsKey(leaderboardId)
               || ProfileLeaderboards.ContainsKey(leaderboardId);
    }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Leaderboard
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public int Limit { get; set; } = 1000;
    public required string Order { get; set; } = "desc";
    public int ScoreFormat { get; set; } = 1;
    public bool Profile { get; set; } = false;
}
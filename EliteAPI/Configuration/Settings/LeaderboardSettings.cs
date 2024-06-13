namespace EliteAPI.Configuration.Settings; 

public class ConfigLeaderboardSettings {
    public int CompleteRefreshInterval { get; set; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public Dictionary<string, Leaderboard> Leaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> CollectionLeaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> SkillLeaderboards { get; set; } = new();
    public Dictionary<string, Leaderboard> PestLeaderboards { get; set; } = new();
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Leaderboard
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public int Limit { get; set; } = 1000;
    public required string Order { get; set; } = "desc";
    public int ScoreFormat { get; set; } = 1;
}
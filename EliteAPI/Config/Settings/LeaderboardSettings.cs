namespace EliteAPI.Config.Settings; 

public class ConfigLeaderboardSettings {
    public int CompleteRefreshInterval { get; set; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<Leaderboard> Leaderboards { get; set; } = new();
    public Dictionary<string, CollectionLeaderboard> CollectionLeaderboards { get; set; } = new();
    public Dictionary<string, SkillLeaderboard> SkillLeaderboards { get; set; } = new();
}

// ReSharper disable once ClassNeverInstantiated.Global
public class Leaderboard
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public int Limit { get; set; } = 5000;
    public required string Order { get; set; } = "desc";
    public int ScoreFormat { get; set; } = 1;
}

// ReSharper disable once ClassNeverInstantiated.Global
public class CollectionLeaderboard {
    public required string Id { get; set; }
    public required string Title { get; set; }
}

// ReSharper disable once ClassNeverInstantiated.Global
public class SkillLeaderboard {
    public required string Id { get; set; }
    public required string Title { get; set; }
}
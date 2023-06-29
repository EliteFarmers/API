namespace EliteAPI.Config.Settings; 

public class ConfigLeaderboardSettings {
    public int CompleteRefreshInterval { get; set; }
    // ReSharper disable once CollectionNeverUpdated.Global
    public List<Leaderboard> Leaderboards { get; set; } = new();
}
// ReSharper disable once ClassNeverInstantiated.Global
public class Leaderboard
{
    public required string Id { get; set; }
    public required string Title { get; set; }
    public int Limit { get; set; }
    public required string Order { get; set; }
    public int ScoreFormat { get; set; }
    public required string Path { get; set; }
    public required string OrderBy { get; set; }
}


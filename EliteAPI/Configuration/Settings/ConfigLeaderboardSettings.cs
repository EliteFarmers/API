namespace EliteAPI.Configuration.Settings;

public class ConfigLeaderboardSettings
{
    public const string LeaderboardSettingsName = "Leaderboards";
    public int CompleteRefreshInterval { get; set; } = 15;
    public int BatchIntervalSeconds { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 1000;
    public int QueueCapacity { get; set; } = 10_000;
    public bool EnableAsyncUpdates { get; set; } = true;
}

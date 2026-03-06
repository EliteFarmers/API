namespace EliteAPI.Configuration.Settings;

public class ConfigBazaarSnapshotSettings
{
	public bool CleanupEnabled { get; set; } = true;
	public int CleanupIntervalHours { get; set; } = 24;
	public int DownsampleAfterDays { get; set; } = 30;
	public int DownsampleBucketMinutes { get; set; } = 60;
}

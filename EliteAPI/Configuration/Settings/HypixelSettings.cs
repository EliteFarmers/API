namespace EliteAPI.Configuration.Settings;

public class HypixelSettings
{
	public const string SectionName = "Hypixel";

	public string ApiKey { get; set; } = string.Empty;
	public string BaseUrl { get; set; } = "https://api.hypixel.net/";
}

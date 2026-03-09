namespace EliteAPI.Configuration.Settings;

public class DiscordSettings
{
	public const string SectionName = "Discord";

	public string ClientId { get; set; } = string.Empty;
	public string ClientSecret { get; set; } = string.Empty;
	public string BotToken { get; set; } = string.Empty;
	public string BaseUrl { get; set; } = "https://discord.com/api/v10";
}

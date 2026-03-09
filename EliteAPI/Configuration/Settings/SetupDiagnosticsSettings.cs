namespace EliteAPI.Configuration.Settings;

public class SetupDiagnosticsSettings
{
	public const string SectionName = "SetupDiagnostics";

	public bool LogProfileRequests { get; set; }
	public bool LogWebsiteSecretBypass { get; set; }
	public List<string> PathPrefixes { get; set; } = ["/profiles", "/account", "/auth", "/guilds"];
}

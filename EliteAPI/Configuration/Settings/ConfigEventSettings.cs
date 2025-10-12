namespace EliteAPI.Configuration.Settings;

public class ConfigEventSettings {
	public EventTeamsWordListDto TeamsWordList { get; set; } = new();
}

public class EventTeamsWordListDto {
	public List<string> First { get; set; } = [];
	public List<string> Second { get; set; } = [];
	public List<string> Third { get; set; } = [];
}
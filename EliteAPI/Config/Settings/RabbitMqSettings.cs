namespace EliteAPI.Config.Settings;

public class RabbitMqSettings {
	public string Host { get; set; } = "localhost";
	public string User { get; set; } = "user";
	public string Password { get; set; } = string.Empty;
	public string? ErrorAlertServer { get; set; }
	public string? ErrorAlertChannel { get; set; }
	public string? ErrorAlertPing { get; set; }
}
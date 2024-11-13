namespace EliteAPI.Configuration.Settings;

public class RabbitMqSettings {
	public string Host { get; set; } = "localhost";
	public string User { get; set; } = "user";
	public string Password { get; set; } = string.Empty;
	public int Port { get; set; } = 5672;
	public string? ErrorAlertServer { get; set; }
	public string? ErrorAlertChannel { get; set; }
	public string? ErrorAlertPing { get; set; }
	public string? WipeServer { get; set; }
	public string? WipeChannel { get; set; }
}
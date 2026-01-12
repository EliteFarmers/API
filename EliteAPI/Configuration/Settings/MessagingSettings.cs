namespace EliteAPI.Configuration.Settings;

public class MessagingSettings
{
	public string? ErrorAlertServer { get; set; }
	public string? ErrorAlertChannel { get; set; }
	public string? ErrorAlertPing { get; set; }
	public string? WipeServer { get; set; }
	public string? WipeChannel { get; set; }
	public string? AuditLogServer { get; set; }
	public string? AuditLogChannel { get; set; }
	public string ChannelName { get; set; } = "eliteapi_messages";
}
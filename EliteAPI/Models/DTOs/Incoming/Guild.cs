namespace EliteAPI.Models.DTOs.Incoming;

public class IncomingGuildDto {
	public string? Id { get; set; }
	public required string Name { get; set; }
	public string? Icon { get; set; }
	public string? Banner { get; set; }
	public string? Permissions { get; set; }
	public string? BotPermissions { get; set; }

	public List<string>? Features { get; set; }
	public List<IncomingGuildChannelDto>? Channels { get; set; }
	public List<IncomingGuildRoleDto>? Roles { get; set; }
}

public class IncomingGuildChannelDto {
	public required string Id { get; set; }
	public required string Name { get; set; }
	public int Type { get; set; }
	public int Position { get; set; }
	public string? Permissions { get; set; }
}

public class IncomingGuildRoleDto {
	public required string Id { get; set; }
	public required string Name { get; set; }
	public int Position { get; set; }
}
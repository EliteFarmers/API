namespace EliteAPI.Models.DTOs.Incoming; 

public class IncomingAccountDto {
    public required ulong Id { get; set; }
    public required string Username { get; set; }
    public string? DisplayName { get; set; }
    public string? Discriminator { get; set; }
    public string? Avatar { get; set; }
    public string? Locale { get; set; }
}

public class IncomingGuildDto {
    public string? Id { get; set; }
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public string? Banner { get; set; }
    public string? Permissions { get; set; }
    public List<string>? Features { get; set; }
    public List<IncomingGuildChannelDto>? Channels { get; set; }
}

public class IncomingGuildChannelDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Type { get; set; }
    public int Position { get; set; }
    public string? Permissions { get; set; }
}

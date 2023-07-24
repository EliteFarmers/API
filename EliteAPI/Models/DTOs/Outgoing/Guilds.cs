using EliteAPI.Models.DTOs.Incoming;
using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class GuildDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public GuildFeatures Features { get; set; } = new();

    public string? Icon { get; set; }
    public string? InviteCode { get; set; }
    public string? Banner { get; set; }
    
    public string? Description { get; set; }

    public string? AdminRole { get; set; }
    
    public string? BotPermissions { get; set; }
    public required string BotPermissionsNew { get; set; }
    
    public List<string> DiscordFeatures { get; set; } = new();
}

public class UserGuildDto {
    public required string Id { get; set; }
    public required string Name { get; set; }

    public string? Icon { get; set; }
    public bool HasBot { get; set; }
    public required string Permissions { get; set; }
}

public class AuthorizedGuildDto {
    public required string Id { get; set; }
    public required string Permissions { get; init; }

    public GuildDto? Guild { get; set; }
    public FullDiscordGuild? DiscordGuild { get; set; }
}
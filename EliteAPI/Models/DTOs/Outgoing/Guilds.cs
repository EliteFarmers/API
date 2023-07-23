using EliteAPI.Models.Entities.Events;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class GuildDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public GuildFeatures Features { get; set; } = new();

    public string? InviteCode { get; set; }
    public string? Banner { get; set; }
    
    public string? Description { get; set; }

    public ulong AdminRole { get; set; }

    public string? Icon { get; set; }
    public uint BotPermissions { get; set; }
    public required string BotPermissionsNew { get; set; }
    
    public List<string> DiscordFeatures { get; set; } = new();
}
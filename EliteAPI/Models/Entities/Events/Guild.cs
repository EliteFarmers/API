using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Events; 

public class Guild {
    [Key]
    public ulong Id { get; set; }
    public required string Name { get; set; }
    
    public string? InviteCode { get; set; }
    public string? Banner { get; set; }
    
    [MaxLength(1024)]
    public string? Description { get; set; }

    public ulong AdminRole { get; set; }
}

public class UserIdentification {
    public string? DiscordId { get; set; }
    public string? Uuid { get; set; }
    public string? Ign { get; set; }
}

public class DiscordRole {
    public required string Name { get; set; }
    public required string Id { get; set; }
}

public class BlockedUser {
    public string? DiscordId { get; set; }
    public string? Uuid { get; set; }
    public string? Username { get; set; }
    
    [MaxLength(128)]
    public string? Reason { get; set; }
    public DateTimeOffset? Expiration { get; set; }
    public DateTimeOffset? Created { get; set; }
}
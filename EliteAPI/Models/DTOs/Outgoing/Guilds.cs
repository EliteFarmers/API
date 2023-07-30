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

public class PublicGuildDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public string? Icon { get; set; }
    public string? Banner { get; set; }
    public string? InviteCode { get; set; }
    
    public string? Description { get; set; }
    
    public PublicGuildFeaturesDto Features { get; set; } = new();
}

public class PublicGuildFeaturesDto {
    public bool JacobLeaderboardEnabled { get; set; }
    public PublicJacobLeaderboardFeatureDto? JacobLeaderboard { get; set; }
}

public class PublicJacobLeaderboardFeatureDto {
    public int MaxLeaderboards { get; set; } = 1;
    
    public List<DiscordRole> BlockedRoles { get; set; } = new();
    public List<DiscordRole> RequiredRoles { get; set; } = new();
    
    public List<ExcludedTimespan> ExcludedTimespans { get; set; } = new();
    
    public List<PublicJacobLeaderboardDto> Leaderboards { get; set; } = new();
}

public class PublicJacobLeaderboardDto {
    public required string Id { get; set; }
    public string? ChannelId { get; set; }

    public long StartCutoff { get; set; } = -1;
    public long EndCutoff { get; set; } = -1;

    public string? Title { get; set; }
    public bool Active { get; set; } = true;
    
    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }
    
    public string? UpdateChannelId { get; set; }
    public string? UpdateRoleId { get; set; }
    public bool PingForSmallImprovements { get; set; }
    
    public CropRecords Crops { get; set; } = new();
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
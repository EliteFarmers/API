using EliteAPI.Models.Entities.Discord;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class PrivateGuildDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool Public { get; set; }
    
    public GuildFeatures Features { get; set; } = new();

    public string? Icon { get; set; }
    public string? InviteCode { get; set; }
    public string? Banner { get; set; }
    
    public string? Description { get; set; }

    public string? AdminRole { get; set; }

    public string? BotPermissions { get; set; }
    public required string BotPermissionsNew { get; set; }
    
    public List<string> DiscordFeatures { get; set; } = new();
    
    public int MemberCount { get; set; }
    
    public List<GuildChannelDto> Channels { get; set; } = new();
    public List<GuildRoleDto> Roles { get; set; } = new();
}

public class PublicGuildDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public string? Icon { get; set; }
    public string? Banner { get; set; }
    public string? InviteCode { get; set; }
    
    public string? Description { get; set; }
    public int MemberCount { get; set; }
    
    public PublicGuildFeaturesDto Features { get; set; } = new();
}

public class GuildChannelDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Type { get; set; }
    public int Position { get; set; }
}

public class GuildRoleDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
}

public class PublicGuildFeaturesDto {
    public bool JacobLeaderboardEnabled { get; set; }
    public PublicJacobLeaderboardFeatureDto? JacobLeaderboard { get; set; }
    
    public bool EventsEnabled { get; set; }
    public GuildEventSettings EventSettings { get; set; }
    
    public bool ContestPingsEnabled { get; set; }
    public ContestPingsFeatureDto? ContestPings { get; set; }
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

public class ContestPingsFeatureDto {
    public bool Enabled { get; set; }

    public string? GuildId { get; set; }
    public string? ChannelId { get; set; }
    public string? AlwaysPingRole { get; set; }
    public CropSettings<string>? CropPingRoles { get; set; } = new();

    public int DelaySeconds { get; set; }
    public string? DisabledReason { get; set; }
}

public class GuildDetailsDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public string? Icon { get; set; }
    public string? Banner { get; set; }
    public string? InviteCode { get; set; }
    
    public int MemberCount { get; set; }
}

public class GuildMemberDto {
    public required string Id { get; set; }
    public required string Name { get; set; }

    public string? Icon { get; set; }
    public bool HasBot { get; set; }
    public required string Permissions { get; set; }
    public List<string> Roles { get; set; } = [];
}

public static class GuildMemberDtoExtensions {
    public static GuildMemberDto ToDto(this GuildMember member) {
        return new GuildMemberDto {
            Id = member.GuildId.ToString(),
            Name = member.Guild.Name,
            Icon = member.Guild.Icon,
            HasBot = member.Guild.HasBot,
            Permissions = member.Permissions.ToString(),
            Roles = member.Roles.Select(r => r.ToString()).ToList()
        };
    }
}

public class AuthorizedGuildDto {
    public required string Id { get; set; }
    public required string Permissions { get; init; }

    public PrivateGuildDto? Guild { get; set; }
    public GuildMemberDto? Member { get; set; }
}
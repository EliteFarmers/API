using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Models.Entities.Discord; 

public class Guild {
    [Key]
    public ulong Id { get; set; }
    public required string Name { get; set; }
    
    [Column(TypeName = "jsonb")]
    public GuildFeatures Features { get; set; } = new();
    
    public string? InviteCode { get; set; }
    public string? Banner { get; set; }
    
    [MaxLength(1024)]
    public string? Description { get; set; }

    public ulong AdminRole { get; set; }
    
    public string? Icon { get; set; }
    public ulong BotPermissions { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> DiscordFeatures { get; set; } = [];
    
    public int MemberCount { get; set; }
    public bool HasBot { get; set; }
    public bool IsPublic { get; set; } = false;
    
    public List<GuildChannel> Channels { get; set; } = [];
    public List<GuildRole> Roles { get; set; } = [];
    public List<GuildEntitlement> Entitlements { get; set; } = [];
    
    public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
}

public class GuildFeatures {
    public bool Locked { get; set; } 
    public bool JacobLeaderboardEnabled { get; set; }
    public GuildJacobLeaderboardFeature? JacobLeaderboard { get; set; }
    
    public bool VerifiedRoleEnabled { get; set; }
    public VerifiedRoleFeature? VerifiedRole { get; set; }
    
    public bool EventsEnabled { get; set; }
    public GuildEventSettings? EventSettings { get; set; } = new();

    public bool ContestPingsEnabled { get; set; } = true;
    public ContestPingsFeature? ContestPings { get; set; }
}

public class GuildEventSettings {
    public int MaxMonthlyEvents { get; set; } = 1;
    public bool PublicEventsEnabled { get; set; }
    public List<EventCreatedDto> CreatedEvents { get; set; } = new();
}

public class EventCreatedDto {
    public required string Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class GuildJacobLeaderboardFeature {
    public int MaxLeaderboards { get; set; } = 1;
    
    public List<DiscordRole> BlockedRoles { get; set; } = new();
    public List<ulong> BlockedUsers { get; set; } = new();
    public List<DiscordRole> RequiredRoles { get; set; } = new();
    
    public List<string> ExcludedParticipations { get; set; } = new();
    public List<ExcludedTimespan> ExcludedTimespans { get; set; } = new();
    
    public List<GuildJacobLeaderboard> Leaderboards { get; set; } = new();
}

public class ContestPingsFeature {
    public bool Enabled { get; set; }

    public string? ChannelId { get; set; }
    public string? AlwaysPingRole { get; set; }
    public CropSettings<string>? CropPingRoles { get; set; } = new();

    public int DelaySeconds { get; set; }
    public string? DisabledReason { get; set; }
}

public class GuildJacobLeaderboard {
    public required string Id { get; set; }
    public string? ChannelId { get; set; }

    public long StartCutoff { get; set; } = -1;
    public long EndCutoff { get; set; } = -1;

    [MaxLength(64)] 
    public string? Title { get; set; }
    public bool Active { get; set; } = true;

    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }

    public string? UpdateChannelId { get; set; }
    public string? UpdateRoleId { get; set; }
    public bool PingForSmallImprovements { get; set; }

    public CropRecords Crops { get; set; } = new();
}

public class UpdateGuildJacobLeaderboardDto {
    public string? ChannelId { get; set; }

    public long? StartCutoff { get; set; }
    public long? EndCutoff { get; set; }

    [MaxLength(64)] 
    public string? Title { get; set; }

    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }

    public string? UpdateChannelId { get; set; }
    public string? UpdateRoleId { get; set; }
    public bool? PingForSmallImprovements { get; set; }
}

public class CropSettings<T> {
    public T? Cactus { get; set; }
    public T? Carrot { get; set; }
    public T? Potato { get; set; }
    public T? Wheat { get; set; }
    public T? Melon { get; set; }
    public T? Pumpkin { get; set; }
    public T? Mushroom { get; set; }
    public T? CocoaBeans { get; set; }
    public T? SugarCane { get; set; }
    public T? NetherWart { get; set; }
}

public class CropRecords {
    public List<GuildJacobLeaderboardEntry> Cactus { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> Carrot { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> Potato { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> Wheat { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> Melon { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> Pumpkin { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> Mushroom { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> CocoaBeans { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> SugarCane { get; set; } = new();
    public List<GuildJacobLeaderboardEntry> NetherWart { get; set; } = new();
}

public class GuildJacobLeaderboardEntry {
    public required string Uuid { get; set; }
    public required string Ign { get; set; }
    public required string DiscordId { get; set; }
    
    public required ContestParticipationDto Record { get; set; }
}

public class ExcludedTimespan {
    public long Start { get; set; }
    public long End { get; set; }
    public string? Reason { get; set; }
}

public class VerifiedRoleFeature {
    public bool Enabled { get; set; }
    public List<AutoRoles> AutoRoles { get; set; } = new();
}

public class AutoRoles {
    public string? RoleId { get; set; }
    public int RequiredWeight { get; set; }
}

public class UserIdentification {
    public string? DiscordId { get; set; }
    public string? Uuid { get; set; }
    public string? Ign { get; set; }
}

public class DiscordRole {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public int Position { get; set; }
    public ulong Permissions { get; set; }
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.DTOs.Outgoing;

namespace EliteAPI.Models.Entities.Events; 

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
    public string? BotPermissionsNew { get; set; }

    [Column(TypeName = "jsonb")]
    public List<string> DiscordFeatures { get; set; } = new();
    
    public int MemberCount { get; set; }
}

public class GuildFeatures {
    public bool JacobLeaderboardEnabled { get; set; }
    public GuildJacobLeaderboardFeature? JacobLeaderboard { get; set; }
    
    public bool VerifiedRoleEnabled { get; set; }
    public VerifiedRoleFeature? VerifiedRole { get; set; }
    
    public bool EventsEnabled { get; set; }
    public GuildEventSettings EventSettings { get; set; } = new();
}

public class GuildEventSettings {
    public int MaxEvents { get; set; } = 1;
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

public class GuildJacobLeaderboard {
    public required string Id { get; set; }
    public string? ChannelId { get; set; }

    public long StartCutoff { get; set; } = -1;
    public long EndCutoff { get; set; } = -1;

    [MaxLength(64)] public string? Title { get; set; }
    public bool Active { get; set; } = true;

    public string? RequiredRole { get; set; }
    public string? BlockedRole { get; set; }

    public string? UpdateChannelId { get; set; }
    public string? UpdateRoleId { get; set; }
    public bool PingForSmallImprovements { get; set; }

    public CropRecords Crops { get; set; } = new();
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
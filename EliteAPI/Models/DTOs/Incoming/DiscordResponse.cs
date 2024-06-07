using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Incoming;

public class DiscordUserResponse
{
    public ulong Id { get; set; }
    public required string Username { get; set; }

    public string? Discriminator { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public string? Locale { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

public class RefreshTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    public string? Error { get; set; }
}

public class DiscordGuild {
    public ulong Id { get; set; }
    public required string Name { get; set; }
    
    public string? Icon { get; set; }
    public bool HasBot { get; set; }
    public ulong Permissions { get; set; }
    
    [JsonPropertyName("permissions_new")]
    public string? PermissionsNew { get; set; }
    public List<string> Features { get; set; } = new();
    
    [JsonPropertyName("approximate_member_count")]
    public int MemberCount { get; set; }
}

public class FullDiscordGuild 
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Icon { get; set; }
    public string? Description { get; set; }
    public string? Splash { get; set; }
    
    [JsonPropertyName("discovery_splash")]
    public string? DiscoverySplash { get; set; }

    public List<string> Features { get; set; } = new();

    public string? Banner { get; set; }
    [JsonPropertyName("owner_id")]
    public string? OwnerId { get; set; }
    [JsonPropertyName("application_id")]
    public int? ApplicationId { get; set; }
    
    public string? Region { get; set; }
    
    [JsonPropertyName("verification_level")]
    public int VerificationLevel { get; set; }
    
    public List<DiscordRoleData> Roles { get; set; } = new();
    public List<DiscordChannel> Channels { get; set; } = new();

    [JsonPropertyName("vanity_url_code")]
    public string? VanityUrlCode { get; set; }
    
    [JsonPropertyName("premium_tier")]
    public int PremiumTier { get; set; }
    [JsonPropertyName("premium_subscription_count")]
    public int PremiumSubscriptionCount { get; set; }

    [JsonPropertyName("preferred_locale")]
    public string? PreferredLocale { get; set; }
    
    [JsonPropertyName("approximate_member_count")]
    public int MemberCount { get; set; }
}

public class DiscordRoleData
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public string? Permissions { get; set; }
    public int Position { get; set; }
    public int Color { get; set; }
}

public class DiscordGuildMember {
    public List<string> Roles { get; set; } = [];
}

public class DiscordChannel
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public ChannelType Type { get; set; }
    public int Flags { get; set; }
    public int Position { get; set; }
    
    [JsonPropertyName("guild_id")]
    public string? GuildId { get; set; }
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }
}

public enum ChannelType {
    GuildText = 0,
    DirectMessage = 1,
    GuildVoice = 2,
    GroupDirectMessage = 3,
    GuildCategory = 4,
    GuildAnnouncement = 5,
    AnnouncementThread = 10,
    PublicThread = 11,
    PrivateThread = 12,
    GuildStage = 13,
    GuildDirectory = 14,
    GuildForum = 15
}
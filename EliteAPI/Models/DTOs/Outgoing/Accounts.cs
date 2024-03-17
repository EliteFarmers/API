using EliteAPI.Models.DTOs.Incoming;
using Swashbuckle.AspNetCore.Annotations;

namespace EliteAPI.Models.DTOs.Outgoing;

[SwaggerSchema(Required = new [] { "Id", "DisplayName", "Username", "Redemptions", "Inventory", "Settings", "MinecraftAccounts" })]
public class AuthorizedAccountDto
{
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    
    public int Permissions { get; set; } = 0;
    
    public string? Discriminator { get; set; }
    public string? Email { get; set; }
    public string? Locale { get; set; }
    public string? Avatar { get; set; }

    public List<RedemptionDto> Redemptions { get; set; } = new();
    public EliteInventoryDto Inventory { get; set; } = new();
    public EliteSettingsDto Settings { get; set; } = new();
    public List<MinecraftAccountDetailsDto> MinecraftAccounts { get; set; } = new();
}

[SwaggerSchema(Required = new [] { "Id", "Name", "Properties", "PrimaryAccount" })]
public class MinecraftAccountDetailsDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool PrimaryAccount { get; set; } = true;
    public List<UserBadgeDto> Badges { get; set; } = new();
    public List<MinecraftAccountPropertyDto> Properties { get; set; } = new();
}

[SwaggerSchema(Required = new [] { "Id", "Name", "Properties", "Profiles", "PrimaryAccount", "EventEntries" })]
public class MinecraftAccountDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public bool PrimaryAccount { get; set; } = true;
    
    public string? DiscordId { get; set; }
    public string? DiscordUsername { get; set; }
    public string? DiscordAvatar { get; set; }

    public List<MinecraftAccountPropertyDto> Properties { get; set; } = new();
    public List<ProfileDetailsDto> Profiles { get; set; } = new();
    public List<UserBadgeDto> Badges { get; set; } = new();
    public PlayerDataDto? PlayerData { get; set; }
}

[SwaggerSchema(Required = new [] { "Name", "Value" })]
public class MinecraftAccountPropertyDto
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

[SwaggerSchema(Required = new [] { "TotalEarnedMedals", "SpentMedals", "EventTokens", "EventTokensSpent", "LeaderboardTokens", "LeaderboardTokensSpent", "UnlockedCosmetics" })]
public class EliteInventoryDto
{
    public MedalInventoryDto TotalEarnedMedals { get; set; } = new();
    public MedalInventoryDto SpentMedals { get; set; } = new();

    public int EventTokens { get; set; } = 0;
    public int EventTokensSpent { get; set; } = 0;

    public int LeaderboardTokens { get; set; } = 0;
    public int LeaderboardTokensSpent { get; set; } = 0;

    public List<string> UnlockedCosmetics { get; set; } = new();
}

public class EliteSettingsDto
{
    public string DefaultPlayerUuid { get; set; } = string.Empty;
    public bool HideDiscordTag { get; set; } = false;
}

public class RedemptionDto
{
    public required string ItemId { get; set; }
    public required string Cost { get; set; }
    public DateTime Timestamp { get; set; }
}

public class LinkedAccountsDto {
    public string? SelectedUuid { get; set; }
    public List<PlayerDataDto> Players { get; set; } = new();
}

public enum PurchaseType
{
    Donation = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
}

public class AccountWithPermsDto {
    public required string Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }

    public int Permissions { get; set; } = 0;

    public string? Discriminator { get; set; }
    public string? Avatar { get; set; }
}
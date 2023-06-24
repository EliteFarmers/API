using EliteAPI.Models.Entities;
using System.Text.Json;

namespace EliteAPI.Models.DTOs.Outgoing;

public class AccountDto
{
    public ulong Id { get; set; }
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public string? Discriminator { get; set; }
    public string? Email { get; set; }
    public string? Locale { get; set; }

    public List<RedemptionDto> Redemptions { get; set; } = new();
    public EliteInventoryDto Inventory { get; set; } = new();
    public EliteSettingsDto Settings { get; set; } = new();

    public List<MinecraftAccountDto> MinecraftAccounts { get; set; } = new();
}

public class MinecraftAccountDto
{
    public required string Id { get; set; }
    public required string Name { get; set; }

    public List<MinecraftAccountPropertyDto> Properties { get; set; } = new();
    public List<ProfileMemberDto> Profiles { get; set; } = new();
    // public PlayerData PlayerData { get; set; } = new();
}

public class MinecraftAccountPropertyDto
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

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

public enum PurchaseType
{
    Donation = 0,
    Bronze = 1,
    Silver = 2,
    Gold = 3,
}
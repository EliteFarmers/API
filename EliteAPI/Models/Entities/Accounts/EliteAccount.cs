﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;
using EliteAPI.Models.Entities.Monetization;

namespace EliteAPI.Models.Entities.Accounts; 

[Table("Accounts")]
public class EliteAccount
{
    [Key]
    public required ulong Id { get; set; }
    
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public string? Discriminator { get; set; } = "0";
    public string? Avatar { get; set; }
    public string? Email { get; set; }
    public string? Locale { get; set; }
    
    [ForeignKey("UserSettings")]
    public int? UserSettingsId { get; set; }
    public UserSettings UserSettings { get; set; } = new();
    
    public List<UserEntitlement> Entitlements { get; set; } = [];
    public List<MinecraftAccount> MinecraftAccounts { get; set; } = [];
}

[Flags]
public enum PermissionFlags : ushort {
    None = 0,
    Helper = 16,
    ViewGraphs = 17,
    Moderator = 32,
    Admin = 64
}

public class Purchase
{
    public PurchaseType PurchaseType { get; set; }
    public DateTime Timestamp { get; set; }
    public decimal Price { get; set; } = 0;
}

public class Redemption
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

public class EliteInventory
{
    public MedalInventory TotalEarnedMedals { get; set; } = new();
    public MedalInventory SpentMedals { get; set; } = new();

    public int EventTokens { get; set; } = 0;
    public int EventTokensSpent { get; set; } = 0;

    public int LeaderboardTokens { get; set; } = 0;
    public int LeaderboardTokensSpent { get; set; } = 0;

    public List<string> UnlockedCosmetics { get; set; } = new();
}

public class EliteSettings
{
    public string DefaultPlayerUuid { get; set; } = string.Empty;
    public bool HideDiscordTag { get; set; } = false;
}
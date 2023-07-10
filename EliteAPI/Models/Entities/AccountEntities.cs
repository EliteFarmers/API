using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace EliteAPI.Models.Entities;

public class AccountEntities
{
    [Key]
    public required ulong Id { get; set; }
    public int Permissions { get; set; } = 0;
    
    public required string DisplayName { get; set; }
    public required string Username { get; set; }
    public string? Discriminator { get; set; } = "0";

    public string? Avatar { get; set; }
    public string? Email { get; set; }
    public string? Locale { get; set; }

    [Column(TypeName = "jsonb")]
    public List<Purchase> Purchases { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public List<Redemption> Redemptions { get; set; } = new();
    [Column(TypeName = "jsonb")] 
    public EliteInventory Inventory { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public EliteSettings Settings { get; set; } = new();
    
    public List<MinecraftAccount> MinecraftAccounts { get; set; } = new();
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

public class MinecraftAccount
{
    [Key]
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public bool Selected { get; set; } = false;

    public List<ProfileMember> Profiles { get; set; } = new();
    public PlayerData? PlayerData { get; set; }

    [Column(TypeName = "jsonb")]
    public List<MinecraftAccountProperty> Properties { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, long> PreviousNames { get; set; } = new();

    public long LastUpdated { get; set; }
}

public class MinecraftAccountProperty
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}
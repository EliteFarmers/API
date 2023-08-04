using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities;

public class AccountEntity
{
    [Key]
    public required ulong Id { get; set; }
    public PermissionFlags Permissions { get; set; } = PermissionFlags.None;
    
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

public enum PermissionFlags : ushort {
    None = 0,
    Helper = 16,
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

public class MinecraftAccount
{
    [Key]
    public required string Id { get; set; }
    public required string Name { get; set; }
    public ulong? AccountId { get; set; }
    
    public bool Selected { get; set; }
    
    public PlayerData? PlayerData { get; set; }

    [Column(TypeName = "jsonb")]
    public List<MinecraftAccountProperty> Properties { get; set; } = new();
    
    public AccountFlag Flags { get; set; } = AccountFlag.None;
    public bool IsBanned => Flags.HasFlag(AccountFlag.Banned);
    
    [Column(TypeName = "jsonb")]
    public List<FlagReason>? FlagReasons { get; set; }

    public long LastUpdated { get; set; }
    public long ProfilesLastUpdated { get; set; }
    public long PlayerDataLastUpdated { get; set; }
}

public class MinecraftAccountProperty
{
    public required string Name { get; set; }
    public required string Value { get; set; }
}

public enum AccountFlag : ushort {
    None = 0,
    AutoFlag = 1,
    Banned = 2,
}

public class FlagReason {
    public AccountFlag Flag { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Proof { get; set; } = string.Empty;
    public long Timestamp { get; set; }
}
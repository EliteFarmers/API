using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EliteAPI.Models.Entities.Hypixel;

namespace EliteAPI.Models.Entities.Accounts; 

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
    
    [Column(TypeName = "jsonb")]
    public List<Badge>? Badges { get; set; }

    public long LastUpdated { get; set; }
    public long ProfilesLastUpdated { get; set; }
    public long PlayerDataLastUpdated { get; set; }
}

public class Badge {
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
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
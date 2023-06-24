using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class Profile
{
    [Key]
    public required string ProfileId { get; set; }
    
    public required string ProfileName { get; set; }
    public string? GameMode { get; set; }
    public long LastSave { get; set; }
    public bool IsDeleted { get; set; } = false;

    public List<ProfileMember> Members { get; set; } = new();

    [Column(TypeName = "jsonb")]
    public ProfileBanking Banking { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, int> CraftedMinions { get; set; } = new();
}

public class ProfileMember
{
    [Key] public required Guid Id { get; set; }

    public int SkyblockXp { get; set; } = 0;
    public double Purse { get; set; } = 0;

    public JacobData JacobData { get; set; } = new();
    public Skills Skills { get; set; } = new();
    public bool IsSelected { get; set; } = false;
    public bool WasRemoved { get; set; } = false;
    public long LastUpdated { get; set; } = 0;

    [Column(TypeName = "jsonb")]
    public Dictionary<string, long> Collections { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, int> CollectionTiers { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> Stats { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, int> Essence { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public List<Pet> Pets { get; set; } = new();

    // [ForeignKey("FarmingInventory")]
    // public required string FarmingInventoryId { get; set; }
    // public required FarmingInventory FarmingInventory { get; set; } = new();

    [ForeignKey("MinecraftAccount")]
    public required string PlayerUuid { get; set; }
    public required MinecraftAccount MinecraftAccount { get; set; }

    [ForeignKey("Profile")]
    public required string ProfileId { get; set; }
    public required Profile Profile { get; set; }
}
using EliteAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class Profile
{
    [Key]
    public required string ProfileId { get; set; }
    
    public required string ProfileName { get; set; }
    public string? GameMode { get; set; }
    public DateTime? LastSave { get; set; }
    public List<ProfileMember> Members { get; set; } = new();
    public ProfileBanking Banking { get; set; } = new();
    public bool IsDeleted { get; set; } = false;

    [Column(TypeName = "jsonb")]
    public Dictionary<string, string> CraftedMinions { get; set; } = new();
}

public class ProfileMember
{
    [Key] public required Guid Id { get; set; }
    public required string PlayerUuid { get; set; }

    [Column(TypeName = "jsonb")]
    public Dictionary<string, long> Collections { get; set; } = new();
    [Column(TypeName = "jsonb")]
    public Dictionary<string, double> Stats { get; set; } = new();

    public JacobData JacobData { get; set; } = new();
    public List<Pet> Pets { get; set; } = new();
    public Skills Skills { get; set; } = new();
    public bool IsSelected { get; set; }
    public bool WasRemoved { get; set; } = false;
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    [ForeignKey("MinecraftAccount")]
    public required string MinecraftAccountId { get; set; }
    public required MinecraftAccount MinecraftAccount { get; set; }

    [ForeignKey("Profile")]
    public required string ProfileId { get; set; }
    public required Profile Profile { get; set; }
}
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models.Hypixel;

public class Profile
{
    [Key] public int Id { get; set; }
    public string? ProfileUUID { get; set; }
    public required string ProfileName { get; set; }
    public string? GameMode { get; set; }
    public DateTime? LastSave { get; set; }
    public List<ProfileMember> Members { get; set; } = new();
    public ProfileBanking Banking { get; set; } = new();
    public List<CraftedMinion> CraftedMinions { get; set; } = new(); 
}

public class ProfileMember
{
    [Key] public int Id { get; set; }
    public List<Collection> Collections { get; set; } = new();
    public required JacobData JacobData { get; set; }
    public List<Pet> Pets { get; set; } = new();
    public List<Skill> Skills { get; set; } = new();
    public bool IsSelected { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.MinValue;

    [ForeignKey("PlayerData")]
    public int PlayerDataId { get; set; }
    public required PlayerData PlayerData { get; set; }

    [ForeignKey("MinecraftAccount")]
    public int MinecraftAccountId { get; set; }
    public required MinecraftAccount MinecraftAccount { get; set; }

    [ForeignKey("Profile")]
    public int ProfileId { get; set; }
    public required Profile Profile { get; set; }
}
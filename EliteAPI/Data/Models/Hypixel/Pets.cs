using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models.Hypixel;

public class Pet
{
    [Key] public int Id { get; set; }
    public string? UUID { get; set; }
    public string? Type { get; set; }
    public double Exp { get; set; } = 0;
    public bool Active { get; set; } = false;
    public string? Tier { get; set; }
    public string? HeldItem { get; set; }
    public short CandyUsed { get; set; } = 0;
    public string? Skin { get; set; }

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}

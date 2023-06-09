using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace EliteAPI.Models.Entities.Hypixel;

public class Pet
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Uuid { get; set; }
    public string? Type { get; set; }
    public double Exp { get; set; } = 0;
    public bool Active { get; set; } = false;
    public string? Tier { get; set; }
    public string? HeldItem { get; set; }
    public short CandyUsed { get; set; } = 0;
    public string? Skin { get; set; }

    [ForeignKey("ProfileMember")]
    public Guid ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}

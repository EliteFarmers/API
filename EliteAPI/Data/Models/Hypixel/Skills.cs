using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Data.Models.Hypixel;

public class Skill
{
    [Key] public int Id { get; set; }
    public string? Type { get; set; }
    public long Exp { get; set; } = 0;

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}
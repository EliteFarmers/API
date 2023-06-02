using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class SkillName
{
    public static readonly string Combat = "combat";
    public static readonly string Mining = "mining";
    public static readonly string Foraging = "foraging";
    public static readonly string Fishing = "fishing";
    public static readonly string Enchanting = "enchanting";
    public static readonly string Alchemy = "alchemy";
    public static readonly string Carpentry = "carpentry";
    public static readonly string RuneCrafting = "runecrafting";
    public static readonly string Taming = "taming";
    public static readonly string Farming = "farming";
    public static readonly string Social = "social";
}

public class Skill
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Type { get; set; }
    public double Exp { get; set; } = 0;

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}
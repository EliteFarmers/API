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

public class Skills
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public double Combat { get; set; } = 0;
    public double Mining { get; set; } = 0;
    public double Foraging { get; set; } = 0;
    public double Fishing { get; set; } = 0;
    public double Enchanting { get; set; } = 0;
    public double Alchemy { get; set; } = 0;
    public double Carpentry { get; set; } = 0;
    public double Runecrafting { get; set; } = 0;
    public double Taming { get; set; } = 0;
    public double Farming { get; set; } = 0;
    public double Social { get; set; } = 0;

    [ForeignKey("ProfileMember")]
    public int ProfileMemberId { get; set; }
    public ProfileMember? ProfileMember { get; set; }
}
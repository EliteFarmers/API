using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class SkillName
{
	public const string Combat = "combat";
	public const string Mining = "mining";
	public const string Foraging = "foraging";
	public const string Fishing = "fishing";
	public const string Enchanting = "enchanting";
	public const string Alchemy = "alchemy";
	public const string Carpentry = "carpentry";
	public const string Runecrafting = "runecrafting";
	public const string Taming = "taming";
	public const string Farming = "farming";
	public const string Social = "social";
}

public class Skills
{
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
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

	[ForeignKey("ProfileMember")] public Guid ProfileMemberId { get; set; }
	public ProfileMember? ProfileMember { get; set; }
}
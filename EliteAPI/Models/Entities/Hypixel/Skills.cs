using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

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

	[Column(TypeName = "jsonb")] public Dictionary<string, int> LevelCaps { get; set; } = new();

	[ForeignKey("ProfileMember")] public Guid ProfileMemberId { get; set; }
	public ProfileMember? ProfileMember { get; set; }
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class Profile
{
	[Key] public required string ProfileId { get; set; }

	public required string ProfileName { get; set; }
	public string? GameMode { get; set; }
	public bool IsDeleted { get; set; } = false;
	public double BankBalance { get; set; }
	public double SocialXp { get; set; }

	public List<ProfileMember> Members { get; set; } = new();

	[Column(TypeName = "jsonb")] public Dictionary<string, int> CraftedMinions { get; set; } = new();

	public Garden? Garden { get; set; }

	public long LastUpdated { get; set; }
	public long MuseumLastUpdated { get; set; }
}
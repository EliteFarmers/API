using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Entities.Hypixel;

public class Inventories {
	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }

	public long LastUpdated { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

	public string? Inventory { get; set; }
	public string? EnderChest { get; set; }
	public string? Armor { get; set; }
	public string? Wardrobe { get; set; }
	public string? Equipment { get; set; }
	public string? PersonalVault { get; set; }
	public string? TalismanBag { get; set; }
	[Column(TypeName = "jsonb")] public List<string>? Backpacks { get; set; }

	[ForeignKey("ProfileMember")] public Guid ProfileMemberId { get; set; }
	public ProfileMember ProfileMember { get; set; } = null!;
}
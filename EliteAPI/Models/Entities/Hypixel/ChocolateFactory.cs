using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Models.Entities.Hypixel;

public class ChocolateFactory {
	[Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
	public int Id { get; set; }
	
	public long Chocolate { get; set; }
	public long TotalChocolate { get; set; }
	public long ChocolateSincePrestige { get; set; }
	public long ChocolateSpent { get; set; }
	public long LastViewedChocolateFactory { get; set; }
	public int Prestige { get; set; }
	
	public ChocolateFactoryRabbits UniqueRabbits { get; set; } = new();
	public ChocolateFactoryRabbits TotalRabbits { get; set; } = new();
	
	public bool UnlockedZorro { get; set; }
	
	[ForeignKey("ProfileMember")]
	public Guid ProfileMemberId { get; set; }
	public ProfileMember? ProfileMember { get; set; }
}

[Owned]
public class ChocolateFactoryRabbits {
	public int Common { get; set; }
	public int Uncommon { get; set; }
	public int Rare { get; set; }
	public int Epic { get; set; }
	public int Legendary { get; set; }
	public int Mythic { get; set; }
	public int Divine { get; set; }
}
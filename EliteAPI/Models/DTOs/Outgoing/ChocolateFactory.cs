namespace EliteAPI.Models.DTOs.Outgoing;

public class ChocolateFactoryDto {
	public long Chocolate { get; set; }
	public long TotalChocolate { get; set; }
	public long ChocolateSincePrestige { get; set; }
	public long ChocolateSpent { get; set; }
	public int Prestige { get; set; }
	public long LastViewed { get; set; }
	
	public ChocolateFactoryRabbitsDto UniqueRabbits { get; set; } = new();	
	public ChocolateFactoryRabbitsDto TotalRabbits { get; set; } = new();
}

public class ChocolateFactoryRabbitsDto {
	public int Common { get; set; }
	public int Uncommon { get; set; }
	public int Rare { get; set; }
	public int Epic { get; set; }
	public int Legendary { get; set; }
	public int Mythic { get; set; }
	public int Divine { get; set; }
}
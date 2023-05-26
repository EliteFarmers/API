namespace EliteAPI.Models.DTOs.Outgoing;

public class CollectionDto
{
    public required string Name { get; set; }
    public required long Amount { get; set; }
    public int Tier { get; set; }
}

public class CraftedMinionDto
{
    public string? Type { get; set; }
    public int Tiers { get; set; } = 0;
}

public class PetDto
{
    public string? Uuid { get; set; }
    public required string Type { get; set; }
    public double Exp { get; set; } = 0;
    public bool Active { get; set; } = false;
    public string? Tier { get; set; }
    public string? HeldItem { get; set; }
    public short CandyUsed { get; set; } = 0;
    public string? Skin { get; set; }
}
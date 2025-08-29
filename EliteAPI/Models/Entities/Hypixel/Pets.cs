namespace EliteAPI.Models.Entities.Hypixel;

public class Pet
{
    public string? Uuid { get; set; }
    public string? Type { get; set; }
    public double Exp { get; set; } = 0;
    public bool Active { get; set; } = false;
    public string? Tier { get; set; }
    public string? HeldItem { get; set; }
    public short CandyUsed { get; set; } = 0;
    public string? Skin { get; set; }
    public int Level { get; set; } = 1;
}

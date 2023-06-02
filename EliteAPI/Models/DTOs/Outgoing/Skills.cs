namespace EliteAPI.Models.DTOs.Outgoing;

public class SkillDto
{
    public required string Type { get; set; }
    public double Exp { get; set; } = 0;
}
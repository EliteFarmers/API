using System.ComponentModel.DataAnnotations;

namespace EliteAPI.Models.Hypixel;

public class PlayerData
{
    [Key] public int Id { get; set; }
    public string? Rank { get; set; }
    public string? NewPackageRank { get; set; }
    public string? MonthlyPackageRank { get; set; }
    public string? RankPlusColor { get; set; }
    public string? SocialMedia { get; set; }
}
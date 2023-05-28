using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Models.Hypixel;

public class PlayerData
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public string? Rank { get; set; }
    public string? NewPackageRank { get; set; }
    public string? MonthlyPackageRank { get; set; }
    public string? RankPlusColor { get; set; }
    public string? SocialMedia { get; set; }
}
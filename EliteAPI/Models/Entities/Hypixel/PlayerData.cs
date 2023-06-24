using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Models.Entities.Hypixel;

public class PlayerData
{
    [Key] [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey("MinecraftAccount")]
    public required string Uuid { get; set; }
    public MinecraftAccount? MinecraftAccount { get; set; }

    public string? DisplayName { get; set; }

    public long FirstLogin { get; set; }
    public long LastLogin { get; set; }
    public long LastLogout { get; set; }

    public int Karma { get; set; }
    public double NetworkExp { get; set; }
   
    public int RewardHighScore { get; set; }
    public int RewardScore { get; set; }
    public int RewardStreak { get; set; }
    public int TotalDailyRewards { get; set; }
    public int TotalRewards { get; set; }

    public string? Rank { get; set; }
    public string? NewPackageRank { get; set; }
    public string? RankPlusColor { get; set; }
    public string? MonthlyPackageRank { get; set; }
    public string? MostRecentMonthlyPackageRank { get; set; }
    public string? MonthlyRankColor { get; set; }

    public SocialMediaLinks SocialMedia { get; set; } = new();

    public long LastUpdated { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
}

[Owned]
public class SocialMediaLinks
{
    public string? Discord { get; set; }
    public string? Hypixel { get; set; }
    public string? Youtube { get; set; }
}
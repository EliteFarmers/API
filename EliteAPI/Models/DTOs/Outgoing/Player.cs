using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Outgoing;

public class PlayerDataDto
{
    public required string Uuid { get; set; }

    [JsonPropertyName("displayname")]
    public string? DisplayName { get; set; }

    public long FirstLogin { get; set; }
    public long LastLogin { get; set; }
    public long LastLogout { get; set; }

    public long Karma { get; set; }
    public double NetworkExp { get; set; }
   
    public int RewardHighScore { get; set; }
    public int RewardScore { get; set; }
    public int RewardStreak { get; set; }
    public int TotalDailyRewards { get; set; }
    public int TotalRewards { get; set; }

    public string? Prefix { get; set; }
    public string? Rank { get; set; }
    public string? NewPackageRank { get; set; }
    public string? RankPlusColor { get; set; }
    public string? MonthlyPackageRank { get; set; }
    public string? MostRecentMonthlyPackageRank { get; set; }
    public string? MonthlyRankColor { get; set; }

    public SocialMediaLinksDto? SocialMedia { get; set; }
}

public class SocialMediaLinksDto
{
    public string? Discord { get; set; }
    public string? Hypixel { get; set; }
    public string? Youtube { get; set; }
}

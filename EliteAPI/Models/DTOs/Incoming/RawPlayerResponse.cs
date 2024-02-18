using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Incoming;

public class RawPlayerResponse
{
    public bool Success { get; set; }
    public RawPlayerData? Player { get; set; }
}

public class RawPlayerData
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

    public RawSocialMedia? SocialMedia { get; set; }

    /*
    public long claimed_potato_talisman { get; set; }
    public long skyblock_free_cookie { get; set; }
    public long scorpius_bribe_96 { get; set; }
    public long claimed_century_cake { get; set; }
    */
}

public class RawSocialMedia
{
    public RawSocialMediaLinks? Links { get; set; }
}

public class RawSocialMediaLinks
{
    [JsonPropertyName("DISCORD")]
    public string? Discord { get; set; }
    [JsonPropertyName("HYPIXEL")]
    public string? Hypixel { get; set; }
    [JsonPropertyName("YOUTUBE")]
    public string? Youtube { get; set; }
}
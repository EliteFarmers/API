using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class LeaderboardDto {
    public required string Id { get; set; }
    public required string Title { get; set; }
    public string? ShortTitle { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int MaxEntries { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Profile { get; set; }
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
}

public class LeaderboardEntryDto {
    /// <summary>
    /// Player's IGN if player leaderboard
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Ign { get; init; }
    
    /// <summary>
    /// Player's profile name if player leaderboard
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Profile { get; set; }
    
    /// <summary>
    /// Uuid of the player or profile
    /// </summary>
    public required string Uuid { get; init; }
    
    /// <summary>
    /// Score of the entry
    /// </summary>
    public double Amount { get; init; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Removed { get; init; }
    
    /// <summary>
    /// List of members if profile leaderboard
    /// </summary>
    [JsonIgnore]
    public List<ProfileLeaderboardMemberDto>? Members { get; set; }
    
    [JsonPropertyName("members")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<ProfileLeaderboardMemberDto>? MembersSerializationHelper
    {
        get => Members?.Count > 0 ? Members : null;
        set => Members = value ?? null;
    }
}

public class LeaderboardEntryWithRankDto : LeaderboardEntryDto {
    public int Rank { get; init; } = -1;
}

public class ProfileLeaderboardMemberDto {
    public required string Ign { get; init; }
    public required string Uuid { get; init; }
    /// <summary>
    /// Skyblock xp of the player (used for sorting)
    /// </summary>
    public int Xp { get; init; }
}

public class LeaderboardPositionsDto {
    public Dictionary<string, int> Misc { get; set; } = new();
    public Dictionary<string, int> Skills { get; set; } = new();
    public Dictionary<string, int> Collections { get; set; } = new();
    public Dictionary<string, int> Pests { get; set; } = new();
    public Dictionary<string, int> Profile { get; set; } = new();
}

public class LeaderboardPositionDto {
    public int Rank { get; set; }
    public double Amount { get; set; }
    public int UpcomingRank { get; set; }
    public List<LeaderboardEntryDto>? UpcomingPlayers { get; set; }
}

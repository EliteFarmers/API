﻿namespace EliteAPI.Models.DTOs.Outgoing; 

public class LeaderboardDto {
    public required string Id { get; set; }
    public required string Title { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public int MaxEntries { get; set; }
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
}

public class LeaderboardEntryDto {
    public string? Ign { get; init; }
    public string? Profile { get; init; }
    public string? Uuid { get; init; }
    public double Amount { get; init; }
}

public class LeaderboardEntryWithRankDto {
    public string? Ign { get; init; }
    public string? Profile { get; init; }
    public double Amount { get; init; }
    public int Rank { get; init; }
}

public class LeaderboardPositionsDto {
    public Dictionary<string, int> Misc { get; set; } = new();
    public Dictionary<string, int> Skills { get; set; } = new();
    public Dictionary<string, int> Collections { get; set; } = new();
    public Dictionary<string, int> Pests { get; set; } = new();
}

public class LeaderboardPositionDto {
    public int Rank { get; set; }
    public double Amount { get; set; }
    public int UpcomingRank { get; set; }
    public List<LeaderboardEntryDto>? UpcomingPlayers { get; set; }
}

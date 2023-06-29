using EliteAPI.Services.LeaderboardService;

namespace EliteAPI.Models.DTOs.Outgoing; 

public class LeaderboardDto<T> {
    public required string Id { get; set; }
    public required string Title { get; set; }
    public int Limit { get; set; }
    public int Offset { get; set; }
    public List<LeaderboardEntry<T>> Entries { get; set; } = new();
}
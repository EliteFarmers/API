namespace EliteAPI.Models.DTOs.Outgoing; 

public class YearlyContestsDto {
    public int Year { get; set; }
    public int Count { get; set; }
    public bool Complete { get; set; }
    public Dictionary<long, List<string>> Contests { get; set; } = new();
}
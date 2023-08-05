namespace EliteAPI.Models.DTOs.Outgoing; 

public class EventDetailsDto {
    public required string Id { get; set; }
    public required string Name { get; set; }
    
    public string? Description { get; set; }
    public string? Rules { get; set; }
    public string? PrizeInfo { get; set; }
    
    public string? Banner { get; set; }
    public string? Thumbnail { get; set; }
    
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    
    public bool DynamicStartTime { get; set; }
    public bool Active { get; set; }
    
    public string? GuildId { get; set; }
}
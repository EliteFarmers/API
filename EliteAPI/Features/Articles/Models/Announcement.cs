using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Features.Articles.Models;

public class Announcement
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    public required string Title { get; set; }
    public required string Content { get; set; }
    
    public required AnnouncementType Type { get; set; } = AnnouncementType.Other;
    
    public string? Url { get; set; }
    public DateTimeOffset? TargetStartsAt { get; set; }
    public DateTimeOffset? TargetEndsAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}

public enum AnnouncementType
{
    Other,
    Article,
    News,
    Event,
    Maintenance,
    Shop,
}
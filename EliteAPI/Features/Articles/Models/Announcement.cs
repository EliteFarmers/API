using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EliteAPI.Features.Articles.Models;

public class Announcement
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [MaxLength(256)]
    public required string Title { get; set; }
    [MaxLength(8192)]
    public required string Content { get; set; }
    
    public required AnnouncementType Type { get; set; } = AnnouncementType.Other;
    
    public string? TargetLabel { get; set; }
    public string? TargetUrl { get; set; }
    public DateTimeOffset? TargetStartsAt { get; set; }
    public DateTimeOffset? TargetEndsAt { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
}

public enum AnnouncementType
{
    Other = 0,
    Update = 1,
    Article = 2,
    News = 3,
    Event = 4,
    Maintenance = 5,
    Shop = 6,
}
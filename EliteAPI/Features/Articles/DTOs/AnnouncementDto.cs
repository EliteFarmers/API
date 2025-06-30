using System.Text.Json.Serialization;
using EliteAPI.Features.Articles.Models;
using EliteAPI.Utilities;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Articles;

[Mapper]
public static partial class AnnouncementMapper
{
    public static partial IQueryable<AnnouncementDto> ToDto(this IQueryable<Announcement> q);
    
    [MapperIgnoreSource(nameof(Announcement.Id))]
    public static partial AnnouncementDto ToDto(this Announcement announcement);
}

public class AnnouncementDto
{
    /// <summary>
    /// Announcement title
    /// </summary>
    public required string Title { get; set; }
    /// <summary>
    /// Announcement content
    /// </summary>
    public required string Content { get; set; }
    
    /// <summary>
    /// Type of the announcement
    /// </summary>
    [JsonConverter(typeof(LowercaseEnumConverter<AnnouncementType>))]
    public AnnouncementType Type { get; set; } = AnnouncementType.Other;
    
    /// <summary>
    /// Url to read more about the announcement
    /// </summary>
    public string? Url { get; set; }
    /// <summary>
    /// Optional time stamp for when the topic of the announcement starts
    /// </summary>
    public DateTimeOffset? TargetStartsAt { get; set; }
    /// <summary>
    /// Optional time stamp for when the topic of the announcement ends
    /// </summary>
    public DateTimeOffset? TargetEndsAt { get; set; }
    
    /// <summary>
    /// Announcement creation date
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>
    /// Announcement expiration date (will no longer be shown after this date)
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
}
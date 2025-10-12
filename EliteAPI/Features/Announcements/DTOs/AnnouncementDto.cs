using System.Text.Json.Serialization;
using EliteAPI.Features.Announcements.Models;
using EliteAPI.Utilities;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Announcements;

[Mapper]
public static partial class AnnouncementMapper {
	public static partial IQueryable<AnnouncementDto> ToDto(this IQueryable<Announcement> q);

	public static partial AnnouncementDto ToDto(this Announcement announcement);

	[MapperIgnoreTarget(nameof(Announcement.Id))]
	public static partial Announcement ToModel(this CreateAnnouncementDto dto);
}

public class CreateAnnouncementDto {
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
	public AnnouncementType Type { get; set; } = AnnouncementType.Other;

	/// <summary>
	/// Label for the target of the announcement (e.g. "Read more", "View article")
	/// </summary>
	public string? TargetLabel { get; set; }

	/// <summary>
	/// Url to read more about the announcement
	/// </summary>
	public string? TargetUrl { get; set; }

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

public class AnnouncementDto : CreateAnnouncementDto {
	/// <summary>
	/// Announcement id
	/// </summary>
	public required string Id { get; set; }
}
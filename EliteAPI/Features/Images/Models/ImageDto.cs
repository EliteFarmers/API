using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace EliteAPI.Features.Images.Models;

public class ImageAttachmentDto {
	/// <summary>
	/// Image title
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[MaxLength(64)]
	public string? Title { get; set; }

	/// <summary>
	/// Image description
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	[MaxLength(512)]
	public string? Description { get; set; }

	/// <summary>
	/// Image ordering number
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public int? Order { get; set; }

	/// <summary>
	/// The original width of the image.
	/// </summary>
	public int Width { get; init; }

	/// <summary>
	/// The original height of the image.
	/// </summary>
	public int Height { get; init; }

	/// <summary>
	/// A dictionary of available image sources, keyed by a logical name (e.g., "small", "medium").
	/// </summary>
	public required Dictionary<string, ImageSourceDto> Sources { get; init; }

	/// <summary>
	/// Lowest quality image URL
	/// </summary>
	public string Url { get; set; } = null!;
}

public class ImageSourceDto {
	/// <summary>
	/// The fully-qualified public URL for this image variant.
	/// </summary>
	public required string Url { get; init; }

	/// <summary>
	/// The width of this image variant in pixels.
	/// </summary>
	public int Width { get; init; }
}
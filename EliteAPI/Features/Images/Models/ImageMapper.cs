using System.Diagnostics.CodeAnalysis;
using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Images.Services;
using EliteAPI.Features.Textures.Models;
using Riok.Mapperly.Abstractions;

namespace EliteAPI.Features.Images.Models;

[Mapper]
public static partial class ImageMapper
{
	private static string _baseImageUrl = string.Empty;

	public static void Initialize(ObjectStorageSettings settings) {
		_baseImageUrl = settings.PublicUrl.TrimEnd('/');
	}

	public static string? ToPrimaryUrl(this Image? image) {
		return image is null ? null : BuildUrl(image.Path);
	}
	
	public static string ToUrl(this HypixelItemTexture itemTexture) {
		return BuildUrl(itemTexture.Url);
	}

	public static ImageAttachmentDto? ToDto(this Image? image) {
		if (image is null) return null;

		if (!image.Metadata.TryGetValue("preset", out var presetName) ||
		    !ImagePresets.All.TryGetValue(presetName, out var preset))
			return new ImageAttachmentDto {
				Title = image.Title,
				Description = image.Description,
				Order = image.Order,
				Width = int.TryParse(image.Metadata.GetValueOrDefault("width"), out var wi) ? wi : 0,
				Height = int.TryParse(image.Metadata.GetValueOrDefault("height"), out var hi) ? hi : 0,
				Sources = [],
				Url = BuildUrl(image.Path)
			};

		var sources = new Dictionary<string, ImageSourceDto>();
		var smallestWidth = int.MaxValue;
		var smallestUrl = string.Empty;

		foreach (var variant in preset.Variants) {
			if (!image.Metadata.TryGetValue($"path_{variant.Name}", out var path)) continue;

			sources[variant.Name] = new ImageSourceDto {
				Url = BuildUrl(path),
				Width = variant.Width
			};

			if (variant.Width >= smallestWidth) continue;

			smallestWidth = variant.Width;
			smallestUrl = BuildUrl(path);
		}

		return new ImageAttachmentDto {
			Title = image.Title,
			Description = image.Description,
			Order = image.Order,
			Width = int.TryParse(image.Metadata.GetValueOrDefault("width"), out var w) ? w : 0,
			Height = int.TryParse(image.Metadata.GetValueOrDefault("height"), out var h) ? h : 0,
			Sources = sources,
			Url = smallestUrl
		};
	}

	private static string BuildUrl(string path) {
		var normalizedPath = path.TrimStart('/');
		return string.IsNullOrWhiteSpace(_baseImageUrl)
			? $"/{normalizedPath}"
			: $"{_baseImageUrl}/{normalizedPath}";
	}
}

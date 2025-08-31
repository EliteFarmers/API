using EliteAPI.Features.Images.Services;

namespace EliteAPI.Features.Images.Models;

public static class ImageMapper {
	private static string _baseImageUrl = string.Empty;
	
	public static void Initialize(IConfiguration configuration) {
		_baseImageUrl = configuration["S3:PublicUrl"] 
		                ?? throw new InvalidOperationException("S3:PublicUrl not configured.");
	}

	public static ImageAttachmentDto? ToDto(this Image? image)
	{
		if (image is null) return null;
		
		if (!image.Metadata.TryGetValue("preset", out var presetName) || 
		    !ImagePresets.All.TryGetValue(presetName, out var preset))
		{
			return new ImageAttachmentDto {
				Title = image.Title,
				Description = image.Description,
				Order = image.Order,
				Width = int.TryParse(image.Metadata.GetValueOrDefault("width"), out var wi) ? wi : 0,
				Height = int.TryParse(image.Metadata.GetValueOrDefault("height"), out var hi) ? hi : 0,
				Sources = [],
				Url = $"{_baseImageUrl}/{image.Path}"
			};
		}

		var sources = new Dictionary<string, ImageSourceDto>();
		var smallestWidth = int.MaxValue;
		var smallestUrl = string.Empty;
		
		foreach (var variant in preset.Variants)
		{
			if (!image.Metadata.TryGetValue($"path_{variant.Name}", out var path)) continue;
			
			sources[variant.Name] = new ImageSourceDto {
				Url = $"{_baseImageUrl}/{path}",
				Width = variant.Width
			};
			
			if (variant.Width >= smallestWidth) continue;
			
			smallestWidth = variant.Width;
			smallestUrl = $"{_baseImageUrl}/{path}";
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
}
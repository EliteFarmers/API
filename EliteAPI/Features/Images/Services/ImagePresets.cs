using SixLabors.ImageSharp.Processing;

namespace EliteAPI.Features.Images.Services;

/// <summary>
/// Defines the properties for a single image variant (e.g., large, small, mobile).
/// </summary>
public class ImageVariantDefinition {
	public required string Name { get; init; }
	public required int Width { get; init; }
	public int Quality { get; init; } = 80;
	public ResizeMode Mode { get; init; } = ResizeMode.Max;
}

/// <summary>
/// Defines a complete preset containing multiple variants.
/// </summary>
public class ImagePreset {
	public required List<ImageVariantDefinition> Variants { get; init; }
}

/// <summary>
/// A static registry of all available image processing presets.
/// </summary>
public static class ImagePresets {
	public static readonly Dictionary<string, ImagePreset> All = new() {
		// A multipurpose preset for standard content images
		["standard"] = new ImagePreset {
			Variants =
			[
				new ImageVariantDefinition { Name = "large", Width = 1024 },
				new ImageVariantDefinition { Name = "medium", Width = 512 },
				new ImageVariantDefinition { Name = "small", Width = 256 }
			]
		},
		// A preset for wide, high-resolution hero banners
		["hero"] = new ImagePreset {
			Variants =
			[
				new ImageVariantDefinition { Name = "desktop", Width = 1920, Quality = 85 },
				new ImageVariantDefinition { Name = "tablet", Width = 1280 },
				new ImageVariantDefinition { Name = "mobile", Width = 768 }
			]
		},
		// A preset for small, square icons or avatars
		["icon"] = new ImagePreset {
			Variants =
			[
				new ImageVariantDefinition { Name = "medium", Width = 64, Mode = ResizeMode.Crop },
				new ImageVariantDefinition { Name = "small", Width = 32, Mode = ResizeMode.Crop },
				new ImageVariantDefinition { Name = "tiny", Width = 16, Mode = ResizeMode.Crop }
			]
		},
		// A preset for small, Hypixel item renders
		["item"] = new ImagePreset {
			Variants =
			[
				new ImageVariantDefinition { Name = "default", Width = 64 },
			]
		}
	};

	public static bool IsValid(string presetName) => All.ContainsKey(presetName);
}
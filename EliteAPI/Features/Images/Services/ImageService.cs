using EliteAPI.Services.Interfaces;
using FastEndpoints;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using Image = EliteAPI.Features.Images.Models.Image;

namespace EliteAPI.Features.Images.Services;

public interface IImageService {
	Task<Image> ProcessAndUploadImageAsync(
		IFormFile file,
		string basePath,
		string presetName,
		string? title = null,
		string? description = null,
		CancellationToken token = default
	);

	Task DeleteImageVariantsAsync(Image image);

	Task<Image> CreateImageFromRemoteAsync(string remoteUrl, string basePath, string presetName);
	Task UpdateImageFromRemoteAsync(Image existingImage, string remoteUrl, string basePath, string presetName);
}

[RegisterService<IImageService>(LifeTime.Scoped)]
public class ImageService(
	IObjectStorageService objectStorageService,
	ILogger<IImageService> logger
) : IImageService {
	public async Task<Image> ProcessAndUploadImageAsync(
		IFormFile file,
		string basePath,
		string presetName,
		string? title = null,
		string? description = null,
		CancellationToken token = default) {
		if (!ImagePresets.All.TryGetValue(presetName, out var preset))
			throw new ArgumentException($"Invalid preset: '{presetName}'", nameof(presetName));

		await using var inputStream = file.OpenReadStream();
		using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputStream, token);

		if (image.Frames.Count > 1) ReduceFrameRate(image, 8);

		var imageEntity = new Image {
			Path = $"{basePath}/{preset.Variants.First().Name}.webp",
			Title = title,
			Description = description,
			Metadata = new Dictionary<string, string> {
				{ "width", image.Width.ToString() },
				{ "height", image.Height.ToString() },
				{ "preset", presetName }
			}
		};

		foreach (var variant in preset.Variants) {
			using var outputStream = new MemoryStream();
			using var resizedImage = image.Clone(ctx =>
				ctx.Resize(new ResizeOptions {
					Mode = variant.Mode,
					Size = new Size(variant.Width),
					Sampler = KnownResamplers.NearestNeighbor
				})
			);
			await UploadImageVariantAsync(resizedImage, basePath, variant, imageEntity, token);
		}

		return imageEntity;
	}

	public async Task DeleteImageVariantsAsync(Image image) {
		// Find all metadata keys that represent a file path
		var pathsToDelete = image.Metadata
			.Where(kvp => kvp.Key.StartsWith("path_"))
			.Select(kvp => kvp.Value)
			.ToList();

		// Also include the main path
		pathsToDelete.Add(image.Path);

		foreach (var path in pathsToDelete.Distinct()) {
			if (string.IsNullOrEmpty(path)) continue;

			try {
				await objectStorageService.DeleteAsync(path);
			}
			catch (Exception e) {
				logger.LogWarning(e, "Failed to delete old image variant at {Path}", path);
			}
		}
	}

	private async Task UploadImageVariantAsync(SixLabors.ImageSharp.Image image, string basePath,
		ImageVariantDefinition variant, Image imageEntity, CancellationToken token) {
		var path = $"{basePath}/{variant.Name}.webp";
		await using var memoryStream = new MemoryStream();

		var encoder = new WebpEncoder {
			Quality = variant.Quality,
			SkipMetadata = true,
			Method = WebpEncodingMethod.Level5,
			FileFormat = variant.Width <= 64 ? WebpFileFormatType.Lossless : WebpFileFormatType.Lossy,
			NearLossless = variant.Width <= 128
		};

		await image.SaveAsWebpAsync(memoryStream, encoder, token);
		memoryStream.Position = 0;

		await objectStorageService.UploadAsync(path, memoryStream, token);
		imageEntity.Metadata.Add($"path_{variant.Name}", path);
	}

	public async Task<Image> CreateImageFromRemoteAsync(string remoteUrl, string basePath, string presetName) {
		var formFile = await objectStorageService.DownloadRemoteImageAsync(remoteUrl);
		return await ProcessAndUploadImageAsync(formFile, basePath, presetName);
	}

	public async Task UpdateImageFromRemoteAsync(Image existingImage, string remoteUrl, string basePath,
		string presetName) {
		var formFile = await objectStorageService.DownloadRemoteImageAsync(remoteUrl);

		Image newImage;
		try {
			// Process and upload the new files
			newImage = await ProcessAndUploadImageAsync(formFile, basePath, presetName);
		}
		catch (Exception e) {
			logger.LogError(e, "Failed to process and upload new image from {RemoteUrl}", remoteUrl);
			throw;
		}

		// Delete all the old files associated with the existing image
		await DeleteImageVariantsAsync(existingImage);

		existingImage.Path = newImage.Path;
		existingImage.Metadata = newImage.Metadata;
	}

	private void ReduceFrameRate(SixLabors.ImageSharp.Image image, int maxFps) {
		if (image.Frames.Count <= 1) return;

		// Minimum delay per frame in 1/100s of a second (GIF spec unit)
		var minDelay = (int)Math.Round(100.0 / maxFps);

		// Safeguard: GIF spec minimum is actually 2 (20ms), browsers clamp to ~10 (100ms).
		// We'll enforce at least 2 to avoid "instant" frames.
		var safeMinDelay = Math.Max(2, minDelay);

		var accumulatedDelay = 0;

		for (var i = 0; i < image.Frames.Count; i++) {
			var frame = image.Frames[i];
			var meta = frame.Metadata.GetGifMetadata();

			// Normalize small small frame delays
			var delay = meta.FrameDelay < 2 ? 2 : meta.FrameDelay;
			accumulatedDelay += delay;

			bool keep;
			if (i == 0)
				// Always keep the very first frame
				keep = true;
			else
				keep = accumulatedDelay >= safeMinDelay || i == image.Frames.Count - 1;

			if (keep) {
				meta.FrameDelay = accumulatedDelay;
				accumulatedDelay = 0;
			}
			else {
				image.Frames.RemoveFrame(i);
				i--;
			}
		}
	}
}
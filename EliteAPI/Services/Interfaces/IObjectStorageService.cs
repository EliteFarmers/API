using EliteAPI.Features.Images.Models;

namespace EliteAPI.Services.Interfaces;

public interface IObjectStorageService
{
	Task<string?> UploadAsync(string key, Stream stream, string contentType = "application/octet-stream", CancellationToken token = default);

	Task<Image> UploadImageAsync(string path, IFormFile file, Dictionary<string, string>? metadata = null,
		CancellationToken token = default);

	Task<Image> UploadImageAsync(string path, string remoteUrl, Dictionary<string, string>? metadata = null,
		CancellationToken token = default);

	Task UpdateImageAsync(Image image, IFormFile file, string? newPath = null, CancellationToken token = default);
	Task UpdateImageAsync(Image image, string remoteUrl, string? newPath = null, CancellationToken token = default);
	Task DeleteAsync(string path, CancellationToken token = default);
	Task<string?> GeneratePresignedUrlAsync(string key, TimeSpan expiration, CancellationToken token = default);
	Task<IFormFile> DownloadRemoteImageAsync(string remoteUrl, CancellationToken token = default);
}
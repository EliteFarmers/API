using EliteAPI.Models.Entities.Images;

namespace EliteAPI.Services.Interfaces;

public interface IObjectStorageService {
	Task<string?> UploadAsync(string key, Stream stream, CancellationToken token = default);
	Task<Image> UploadImageAsync(string path, IFormFile file, Dictionary<string, string>? metadata = null, CancellationToken token = default);
	Task<Image> UploadImageAsync(string path, string remoteUrl, Dictionary<string, string>? metadata = null, CancellationToken token = default);
	Task DeleteAsync(string path, CancellationToken token = default);
	Task<string?> GeneratePresignedUrlAsync(string key, TimeSpan expiration, CancellationToken token = default);
}
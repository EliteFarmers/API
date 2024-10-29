using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using EliteAPI.Models.Entities.Images;
using EliteAPI.Services.Interfaces;

namespace EliteAPI.Services;

public class ObjectStorageService : IObjectStorageService {
	private readonly AmazonS3Client? _client;
	private readonly string _bucketName;
	private readonly ILogger<ObjectStorageService> _logger;
	private readonly IHttpClientFactory _httpClientFactory; 

	public ObjectStorageService(IConfiguration config, ILogger<ObjectStorageService> logger, IHttpClientFactory httpClientFactory) {
		_logger = logger;
		_httpClientFactory = httpClientFactory;

		var accessKey = config["S3:AccessKey"];
		var secretKey = config["S3:SecretKey"];
		var endpoint = config["S3:Endpoint"];
		_bucketName = config["S3:BucketName"] ?? "elite";
		
		if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(endpoint)) {
			_logger.LogWarning("S3 credentials not found, ObjectStorageService will not be available.");
			return;
		}
		
		var credentials = new BasicAWSCredentials(accessKey, secretKey);
		_client = new AmazonS3Client(credentials, new AmazonS3Config() {
			ServiceURL = endpoint
		});
	}

	public async Task<string?> UploadAsync(string key, Stream stream, CancellationToken token = default) {
		if (_client is null) return null;
		
		var request = new PutObjectRequest {
			BucketName = _bucketName,
			DisablePayloadSigning = true, // Needed for R2
			InputStream = stream,
			Key = key
		};
		
		var response = await _client.PutObjectAsync(request, token);
		return response.ETag;
	}

	public async Task<Image> UploadImageAsync(string path, IFormFile file, Dictionary<string, string>? metadata = null, CancellationToken token = default) 
	{
		if (_client is null) throw new InvalidOperationException("S3 client not available.");
		
		await using var stream = file.OpenReadStream();
		
		// Get hash of the file
		// var hash = MD5.HashDataAsync(stream, token);
		
		var eTag = await UploadAsync(path, stream, token);
		if (string.IsNullOrEmpty(eTag)) throw new InvalidOperationException("Failed to upload image.");
		
		// var hashString = BitConverter.ToString(await hash).Replace("-", "").ToLower();
		
		var image = new Image {
			Path = path,
			Metadata = metadata ?? new Dictionary<string, string>(),
		};
		
		return image;
	}

	/// <summary>
	/// Downloads an image from a remote URL and uploads it to the object storage
	/// </summary>
	/// <param name="path"></param>
	/// <param name="remoteUrl"></param>
	/// <param name="metadata"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public async Task<Image> UploadImageAsync(string path, string remoteUrl, Dictionary<string, string>? metadata = null, CancellationToken token = default) {
		using var client = _httpClientFactory.CreateClient("EliteAPI");
		var response = await client.GetAsync(remoteUrl, token);
		
		if (!response.IsSuccessStatusCode) {
			throw new InvalidOperationException($"Failed to download image from {remoteUrl}");
		}
		
		// Check if the response is an image
		var contentType = response.Content.Headers.ContentType?.MediaType;
		if (string.IsNullOrEmpty(contentType) || !contentType.StartsWith("image")) {
			throw new InvalidOperationException($"Response from {remoteUrl} is not an image");
		}
		
		await using var stream = await response.Content.ReadAsStreamAsync(token);
		if (stream is null) {
			throw new InvalidOperationException($"Failed to read image from {remoteUrl}");
		}
		
		var formFile = new FormFile(stream, 0, stream.Length, "image", "image" + Path.GetExtension(path));
		return await UploadImageAsync(path, formFile, metadata, token);
	}

	public async Task DeleteAsync(string path, CancellationToken token = default) {
		if (_client is null) return;
		
		var request = new DeleteObjectRequest {
			BucketName = _bucketName,
			Key = path
		};
		
		await _client.DeleteObjectAsync(request, token);
	}

	public async Task<string?> GeneratePresignedUrlAsync(string key, TimeSpan expiration, CancellationToken token = default) {
		if (_client is null) return null;
		
		var request = new GetPreSignedUrlRequest {
			BucketName = _bucketName,
			Key = key,
			Expires = DateTime.UtcNow + expiration,
			Verb = HttpVerb.GET
		};
		
		return await _client.GetPreSignedURLAsync(request);
	}
}
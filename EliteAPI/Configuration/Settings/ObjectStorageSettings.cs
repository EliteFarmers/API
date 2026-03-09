namespace EliteAPI.Configuration.Settings;

public class ObjectStorageSettings
{
	public const string SectionName = "S3";

	public string AccessKey { get; set; } = string.Empty;
	public string SecretKey { get; set; } = string.Empty;
	public string BucketName { get; set; } = string.Empty;
	public string Endpoint { get; set; } = string.Empty;
	public string PublicUrl { get; set; } = string.Empty;
	public bool UseForTextures { get; set; }
}

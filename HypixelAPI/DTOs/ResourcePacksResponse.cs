using System.Text.Json.Serialization;

namespace EliteFarmers.HypixelAPI.DTOs;

public sealed class ResourcePacksResponse
{
	[JsonPropertyName("success")]
	public bool Success { get; set; }

	[JsonPropertyName("packs")]
	public List<ResourcePackResponse> Packs { get; set; } = [];
}

public sealed class ResourcePackResponse
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("lastUpdated")]
	public long LastUpdated { get; set; }

	[JsonPropertyName("deployId")]
	public string DeployId { get; set; } = string.Empty;

	[JsonPropertyName("versions")]
	public List<ResourcePackVersionResponse> Versions { get; set; } = [];
}

public sealed class ResourcePackVersionResponse
{
	[JsonPropertyName("packFormat")]
	public int PackFormat { get; set; }

	[JsonPropertyName("hash")]
	public string Hash { get; set; } = string.Empty;

	[JsonPropertyName("url")]
	public string Url { get; set; } = string.Empty;
}

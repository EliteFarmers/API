using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Auth;

public class DiscordLoginDto {
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; set; }
	[JsonPropertyName("expires_in")]
	public required string ExpiresIn { get; set; }
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}

public class AuthResponseDto {
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; set; }
	[JsonPropertyName("expires_in")]
	public required string ExpiresIn { get; set; }
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}
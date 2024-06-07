using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Auth;

public class DiscordLoginDto {
	/// <summary>
	/// Discord access token from OAuth2
	/// </summary>
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; set; }
	
	/// <summary>
	/// Unix timestamp in seconds
	/// </summary>
	[JsonPropertyName("expires_in")]
	public required string ExpiresIn { get; set; }
	
	/// <summary>
	/// Discord refresh token from OAuth2
	/// </summary>
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}

public class AuthResponseDto {
	/// <summary>
	/// Access token for the user
	/// </summary>
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; set; }
	
	/// <summary>
	/// Expiry date of the access token in Unix timestamp seconds
	/// </summary>
	[JsonPropertyName("expires_in")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? ExpiresIn { get; set; }
	
	/// <summary>
	/// Refresh token for the user
	/// </summary>
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}

public class AuthRefreshDto {
	/// <summary>
	/// User ID
	/// </summary>
	[JsonPropertyName("access_token")]
	public required string AccessToken { get; set; }
	
	/// <summary>
	/// Refresh token for the user
	/// </summary>
	[JsonPropertyName("refresh_token")]
	public required string RefreshToken { get; set; }
}

public class AuthSessionDto {
	/// <summary>
	/// Discord user ID
	/// </summary>
	public required string Id { get; set; }
	/// <summary>
	/// Discord username
	/// </summary>
	public required string Username { get; set; }
	/// <summary>
	/// Discord avatar hash
	/// </summary>
	public required string Avatar { get; set; }
	/// <summary>
	/// Primary Minecraft IGN
	/// </summary>
	public required string Ign { get; set; }
	/// <summary>
	/// List of user roles
	/// </summary>
	public required string[] Roles { get; set; }
}
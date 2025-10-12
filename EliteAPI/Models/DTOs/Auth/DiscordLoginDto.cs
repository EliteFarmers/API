using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Auth;

public class DiscordLoginDto {
	/// <summary>
	/// Discord login code from OAuth2
	/// </summary>
	[JsonPropertyName("code")]
	public required string Code { get; set; }

	/// <summary>
	/// Redirect URI from OAuth2
	/// </summary>
	[JsonPropertyName("redirect_uri")]
	public required string RedirectUri { get; set; }
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
	[JsonPropertyName("user_id")]
	public required string UserId { get; set; }

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
	/// Formatted Primary Minecraft IGN
	/// </summary>
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public string? FIgn { get; set; }

	/// <summary>
	/// Primary Minecraft UUID
	/// </summary>
	public required string Uuid { get; set; }

	/// <summary>
	/// List of user roles
	/// </summary>
	public required string[] Roles { get; set; }
}
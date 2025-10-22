using System.Text.Json.Serialization;

namespace EliteAPI.Features.Auth.Models;

public class AuthRefreshDto
{
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
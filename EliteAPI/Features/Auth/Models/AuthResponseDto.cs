using System.Text.Json.Serialization;
using EliteAPI.Features.Confirmations.Models;

namespace EliteAPI.Features.Auth.Models;

public class AuthResponseDto
{
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
	
	/// <summary>
	/// The pending confirmation for the user, if any
	/// </summary>
	[JsonPropertyName("pending_confirmation")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
	public ConfirmationDto? PendingConfirmation { get; set; }
	
	/// <summary>
	/// If this is the user's first login
	/// </summary>
	[JsonPropertyName("first_login")]
	[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
	public bool FirstLogin { get; set; } = false;
}
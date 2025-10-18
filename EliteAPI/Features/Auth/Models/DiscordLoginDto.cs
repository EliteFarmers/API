using System.Text.Json.Serialization;

namespace EliteAPI.Features.Auth.Models;

public class DiscordLoginDto
{
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
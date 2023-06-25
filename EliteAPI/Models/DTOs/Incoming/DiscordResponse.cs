using System.Text.Json.Serialization;

namespace EliteAPI.Models.DTOs.Incoming;

public class DiscordUserResponse
{
    public ulong Id { get; set; }
    public required string Username { get; set; }

    public string? Discriminator { get; set; }
    public string? Email { get; set; }
    public string? Avatar { get; set; }
    public string? Locale { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }
}

public class RefreshTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    public string? Error { get; set; }
}
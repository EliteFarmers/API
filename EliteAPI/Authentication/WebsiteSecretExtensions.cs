using System.Security.Cryptography;
using System.Text;
using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace EliteAPI.Authentication;

public static class WebsiteSecretExtensions
{
	public const string WebsiteSecretHeaderName = "X-Website-Secret";

	public static bool HasValidWebsiteSecret(this HttpContext? context,
		IOptions<WebsiteGatewaySettings> websiteSettings) {
		return context.HasValidWebsiteSecret(websiteSettings.Value.WebsiteSecret);
	}

	public static bool HasValidWebsiteSecret(this HttpContext? context) {
		if (ConfigGlobalRateLimitSettings.Settings.WebsiteSecret is null) {
			return false;
		}

		return context.HasValidWebsiteSecret(ConfigGlobalRateLimitSettings.Settings.WebsiteSecret);
	}

	public static bool HasValidWebsiteSecret(this HttpContext? context, string expectedSecret) {
		if (context is null || string.IsNullOrWhiteSpace(expectedSecret))
			return false;

		if (!context.Request.Headers.TryGetValue(WebsiteSecretHeaderName, out var headerValue))
			return false;

		return MatchesSecret(expectedSecret, headerValue.ToString());
	}

	private static bool MatchesSecret(string expected, string provided) {
		var expectedBytes = Encoding.UTF8.GetBytes(expected);
		var providedBytes = Encoding.UTF8.GetBytes(provided);
		return expectedBytes.Length == providedBytes.Length &&
		       CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
	}
}
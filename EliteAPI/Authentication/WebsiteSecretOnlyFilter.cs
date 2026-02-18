using System.Security.Cryptography;
using System.Text;
using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace EliteAPI.Authentication;

public class WebsiteSecretOnlyFilter(IOptions<WebsiteGatewaySettings> websiteSettings) : IEndpointFilter
{
	private const string HeaderName = "X-Website-Secret";

	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
		var expectedSecret = websiteSettings.Value.WebsiteSecret;
		if (string.IsNullOrWhiteSpace(expectedSecret))
			return Results.Problem("Website secret is not configured.",
				statusCode: StatusCodes.Status500InternalServerError);

		if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var headerValue))
			return Results.Problem("Missing website secret header.",
				statusCode: StatusCodes.Status403Forbidden);

		var providedSecret = headerValue.ToString();
		if (!MatchesSecret(expectedSecret, providedSecret))
			return Results.Problem("Invalid website secret.",
				statusCode: StatusCodes.Status403Forbidden);

		return await next(context);
	}

	private static bool MatchesSecret(string expected, string provided) {
		var expectedBytes = Encoding.UTF8.GetBytes(expected);
		var providedBytes = Encoding.UTF8.GetBytes(provided);
		return expectedBytes.Length == providedBytes.Length &&
		       CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
	}
}

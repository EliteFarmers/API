using EliteAPI.Configuration.Settings;
using Microsoft.Extensions.Options;

namespace EliteAPI.Authentication;

public class WebsiteSecretOnlyFilter(IOptions<WebsiteGatewaySettings> websiteSettings) : IEndpointFilter
{
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
		var expectedSecret = websiteSettings.Value.WebsiteSecret;
		if (string.IsNullOrWhiteSpace(expectedSecret))
			return Results.Problem("Website secret is not configured.",
				statusCode: StatusCodes.Status500InternalServerError);

		if (!context.HttpContext.Request.Headers.TryGetValue(WebsiteSecretExtensions.WebsiteSecretHeaderName,
			    out _))
			return Results.Problem("Missing website secret header.",
				statusCode: StatusCodes.Status403Forbidden);

		if (!context.HttpContext.HasValidWebsiteSecret(expectedSecret))
			return Results.Problem("Invalid website secret.",
				statusCode: StatusCodes.Status403Forbidden);

		return await next(context);
	}
}

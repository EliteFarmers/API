namespace EliteAPI.Authentication;

public class DiscordBotOnlyFilter : IEndpointFilter {
	public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next) {
		if (context.HttpContext.Request.Headers.Authorization.Count < 1)
			return Results.Problem("Only the bot can access this endpoint.",
				statusCode: StatusCodes.Status403Forbidden);

		var auth = context.HttpContext.Request.Headers.Authorization.ToString();

		// Only allow local requests once bot has moved to share the same network as the API
		if (!auth.StartsWith(
			    "Bearer EliteDiscordBot ") /* || context.HttpContext.Connection.RemoteIpAddress?.IsFromDockerNetwork() != true*/
		   )
			return Results.Problem("Only the bot can access this endpoint.",
				statusCode: StatusCodes.Status403Forbidden);

		if (auth.Replace("Bearer EliteDiscordBot ", "") != Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"))
			return Results.Problem("Only the bot can access this endpoint.",
				statusCode: StatusCodes.Status403Forbidden);

		return await next(context);
	}
}

// sealed class DiscordBotAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,
//     ILoggerFactory logger,
//     UrlEncoder encoder,
//     IConfiguration config)
//     : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
// {
//     internal const string SchemeName = "ApiKey";
//     internal const string HeaderName = "x-api-key";
//     readonly string _apiKey = config["Auth:ApiKey"] ?? throw new InvalidOperationException("Api key not set in appsettings.json");
//
//     protected override Task<AuthenticateResult> HandleAuthenticateAsync()
//     {
//         Request.Headers.TryGetValue(HeaderName, out var extractedApiKey);
//
//         if (!IsPublicEndpoint() && !extractedApiKey.Equals(_apiKey))
//             return Task.FromResult(AuthenticateResult.Fail("Invalid API credentials!"));
//
//         var identity = new ClaimsIdentity(
//             claims: new[] { new Claim("ClientID", "Default") },
//             authenticationType: Scheme.Name);
//         var principal = new GenericPrincipal(identity, roles: null);
//         var ticket = new AuthenticationTicket(principal, Scheme.Name);
//
//         return Task.FromResult(AuthenticateResult.Success(ticket));
//     }
//
//     bool IsPublicEndpoint()
//         => Context.GetEndpoint()?.Metadata.OfType<AllowAnonymousAttribute>().Any() is null or true;
// }
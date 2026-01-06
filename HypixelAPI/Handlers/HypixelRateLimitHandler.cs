using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using EliteFarmers.HypixelAPI.Metrics;

namespace EliteFarmers.HypixelAPI.Handlers;

public class HypixelRateLimitHandler(
	IHypixelRequestLimiter limiter,
	IHypixelKeyUsageCounter keyUsageCounter
) : DelegatingHandler
{
	private const string RateLimitRemaining = "ratelimit-remaining";
	private const string RateLimitLimit = "ratelimit-limit";
	private const string ApiKey = "API-Key";

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken cancellationToken) {
		// Exit early if the request does not contain an API-Key header (these requests don't count against the limit)
		if (request.Headers.TryGetValues(ApiKey, out var apiKeyValues) &&
		    string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault()))
			return await base.SendAsync(request, cancellationToken);

		using var lease = await limiter.AcquireAsync(1, cancellationToken);

		if (!lease.IsAcquired) {
			var limitedResponse = new HttpResponseMessage {
				RequestMessage = new HttpRequestMessage(),
				Content = new StringContent("Rate limit exceeded"),
				StatusCode = HttpStatusCode.TooManyRequests
			};

			if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
				limitedResponse.Headers.Add("Retry-After",
					((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));

			return limitedResponse;
		}

		var endpoint = ExtractEndpoint(request.RequestUri);
		keyUsageCounter.Increment(endpoint);
		var response = await base.SendAsync(request, cancellationToken);

		if (response.Headers.TryGetValues(RateLimitLimit, out var limitValues) &&
		    response.Headers.TryGetValues(RateLimitRemaining, out var remainingValues))
			if (int.TryParse(limitValues.FirstOrDefault(), out var limitInt) &&
			    int.TryParse(remainingValues.FirstOrDefault(), out var remainingInt))
				// Sync the limiter with the authoritative state from the server.
				limiter.Sync(limitInt, remainingInt);

		return response;
	}

	private static string ExtractEndpoint(Uri? uri) {
		if (uri is null) return "unknown";
		
		// Extract the path segment after /v2/ (ex: "skyblock/profiles", "player", "skyblock/garden")
		var path = uri.AbsolutePath;
		const string prefix = "/v2/";
		
		if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) {
			return path[prefix.Length..];
		}
		
		return path.TrimStart('/');
	}
}
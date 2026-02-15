using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using EliteFarmers.HypixelAPI.Metrics;

namespace EliteFarmers.HypixelAPI.Handlers;

public class HypixelRateLimitHandler(
	IHypixelRequestLimiter limiter,
	IHypixelKeyUsageCounter keyUsageCounter,
	IHypixelRequestMetrics requestMetrics
) : DelegatingHandler
{
	private const string RateLimitRemaining = "ratelimit-remaining";
	private const string RateLimitLimit = "ratelimit-limit";
	private const string ApiKey = "API-Key";

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken cancellationToken) {
		var endpoint = ExtractEndpoint(request.RequestUri);
		var method = request.Method.Method;
		var timer = Stopwatch.StartNew();

		// Exit early if the request does not contain an API-Key header (these requests don't count against the limit)
		if (request.Headers.TryGetValues(ApiKey, out var apiKeyValues) &&
		    string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault()))
			return await SendAndRecordAsync(request, endpoint, method, timer, cancellationToken);

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

			requestMetrics.RecordRequest(endpoint, method, (int)limitedResponse.StatusCode,
				timer.Elapsed.TotalMilliseconds, "rate_limited");
			return limitedResponse;
		}

		keyUsageCounter.Increment(endpoint);
		return await SendAndRecordAsync(request, endpoint, method, timer, cancellationToken);
	}

	private async Task<HttpResponseMessage> SendAndRecordAsync(HttpRequestMessage request, string endpoint, string method,
		Stopwatch timer, CancellationToken cancellationToken) {
		try {
			var response = await base.SendAsync(request, cancellationToken);
			requestMetrics.RecordRequest(endpoint, method, (int)response.StatusCode,
				timer.Elapsed.TotalMilliseconds,
				response.IsSuccessStatusCode ? "success" : "http_error");

			if (response.Headers.TryGetValues(RateLimitLimit, out var limitValues) &&
			    response.Headers.TryGetValues(RateLimitRemaining, out var remainingValues))
				if (int.TryParse(limitValues.FirstOrDefault(), out var limitInt) &&
				    int.TryParse(remainingValues.FirstOrDefault(), out var remainingInt))
					// Sync the limiter with the authoritative state from the server.
					limiter.Sync(limitInt, remainingInt);

			return response;
		}
		catch (OperationCanceledException) {
			requestMetrics.RecordRequest(endpoint, method, null, timer.Elapsed.TotalMilliseconds,
				cancellationToken.IsCancellationRequested ? "cancelled" : "timeout");
			throw;
		}
		catch (Exception) {
			requestMetrics.RecordRequest(endpoint, method, null, timer.Elapsed.TotalMilliseconds, "exception");
			throw;
		}
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

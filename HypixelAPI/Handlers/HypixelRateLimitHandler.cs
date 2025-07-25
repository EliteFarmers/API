﻿using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using HypixelAPI.Metrics;

namespace HypixelAPI.Handlers;

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
		if (request.Headers.TryGetValues(ApiKey, out var apiKeyValues) && string.IsNullOrWhiteSpace(apiKeyValues.FirstOrDefault())) {
			return await base.SendAsync(request, cancellationToken);
		}
		
		using var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken);

		if (!lease.IsAcquired) {
			var limitedResponse = new HttpResponseMessage() {
				RequestMessage = new HttpRequestMessage(),
				Content = new StringContent("Rate limit exceeded"),
				StatusCode = HttpStatusCode.TooManyRequests
			};
			
			if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
				limitedResponse.Headers.Add("Retry-After",
					((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
			}

			return limitedResponse;
		}

		keyUsageCounter.Increment();
		var response = await base.SendAsync(request, cancellationToken);

		if (response.Headers.TryGetValues(RateLimitLimit, out var limitValues) &&
		    response.Headers.TryGetValues(RateLimitRemaining, out var remainingValues))
		{
			if (int.TryParse(limitValues.FirstOrDefault(), out var limitInt) &&
			    int.TryParse(remainingValues.FirstOrDefault(), out var remainingInt))
			{
				// Sync the limiter with the authoritative state from the server.
				limiter.Sync(limitInt, remainingInt);
			}
		}
		
		return response;
	}
}
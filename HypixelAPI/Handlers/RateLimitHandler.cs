﻿using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;

namespace HypixelAPI.Handlers;

public class RateLimitHandler(IHypixelRequestLimiter limiter) : DelegatingHandler, IAsyncDisposable
{
	private const string RateLimitRemaining = "ratelimit-remaining";
	private const string RateLimitLimit = "ratelimit-limit";

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
		CancellationToken cancellationToken) {
		using var lease = await limiter.AcquireAsync(permitCount: 1, cancellationToken);

		if (!lease.IsAcquired) {
			var limitedResponse = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
			if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
				limitedResponse.Headers.Add("Retry-After",
					((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo));
			}

			return limitedResponse;
		}

		var response = await base.SendAsync(request, cancellationToken);

		if (!response.Headers.TryGetValues(RateLimitLimit, out var limitValues)) {
			return response;
		}
		
		var limit = limitValues.FirstOrDefault();
		if (!int.TryParse(limit, out var limitInt)) {
			return response;
		}

		if (!response.Headers.TryGetValues(RateLimitRemaining, out var remainingValues)) {
			limiter.UpdateRequestLimit(limitInt);
			return response;
		}
		
		var remaining = remainingValues.FirstOrDefault();
		if (int.TryParse(remaining, out var remainingInt)) {
			limiter.UpdateRequestLimit(limitInt, remainingInt);
		} else {
			limiter.UpdateRequestLimit(limitInt);
		}
		
		return response;
	}

	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		await limiter.DisposeAsync().ConfigureAwait(false);

		Dispose(disposing: false);
		GC.SuppressFinalize(this);
	}
}
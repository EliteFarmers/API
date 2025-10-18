using System.Threading.RateLimiting;
using EliteAPI.Configuration.Settings;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace EliteAPI.RateLimiting;

public class ApiRateLimiterPolicy : IRateLimiterPolicy<string>
{
	private readonly ConfigApiRateLimitSettings _options;
	public Func<OnRejectedContext, CancellationToken, ValueTask>? OnRejected { get; }

	public ApiRateLimiterPolicy(ILogger<ApiRateLimiterPolicy> logger,
		IOptions<ConfigApiRateLimitSettings> options) {
		OnRejected = (ctx, _) => {
			ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
			logger.LogWarning($"Request rejected by {nameof(ApiRateLimiterPolicy)}");

			return ValueTask.CompletedTask;
		};
		_options = options.Value;
	}

	public RateLimitPartition<string> GetPartition(HttpContext httpContext) {
		if (httpContext.Request.Headers.TryGetValue("API-Key", out var apiKey)) {
			// TODO: validate API key and use it as a partition key
		}

		return RateLimitPartition.GetSlidingWindowLimiter(string.Empty,
			_ => new SlidingWindowRateLimiterOptions {
				PermitLimit = _options.PermitLimit,
				QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
				QueueLimit = _options.QueueLimit,
				Window = TimeSpan.FromSeconds(_options.Window),
				SegmentsPerWindow = _options.SegmentsPerWindow
			});
	}
}
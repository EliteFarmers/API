using System.Globalization;
using System.Net;
using System.Threading.RateLimiting;
using EliteAPI.Configuration.Settings;

namespace EliteAPI.Utilities;

public static class RateLimitingExtensions {
	public static IServiceCollection AddEliteRateLimiting(this IServiceCollection services) {
		var globalRateLimitSettings = ConfigGlobalRateLimitSettings.Settings;

		// Build the global limiter and register as singleton
		var globalLimiter = PartitionedRateLimiter.Create<HttpContext, IPAddress>(context => {
			var remoteIp = context.Connection.RemoteIpAddress;

			if (remoteIp is null || remoteIp.IsPrivate()) return RateLimitPartition.GetNoLimiter(IPAddress.Loopback);

			return RateLimitPartition.GetTokenBucketLimiter(remoteIp, _ =>
				new TokenBucketRateLimiterOptions {
					TokenLimit = globalRateLimitSettings.TokenLimit,
					QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
					QueueLimit = globalRateLimitSettings.QueueLimit,
					ReplenishmentPeriod = TimeSpan.FromSeconds(globalRateLimitSettings.ReplenishmentPeriod),
					TokensPerPeriod = globalRateLimitSettings.TokensPerPeriod,
					AutoReplenishment = globalRateLimitSettings.AutoReplenishment
				});
		});

		services.AddSingleton(globalLimiter);
		services.AddSingleton(globalRateLimitSettings);

		return services;
	}

	public static IApplicationBuilder UseEliteRateLimiting(this IApplicationBuilder app) {
		return app.Use(async (context, next) => {
			var limiter = context.RequestServices
				.GetRequiredService<PartitionedRateLimiter<HttpContext>>();
			var options = context.RequestServices
				.GetRequiredService<ConfigGlobalRateLimitSettings>();

			// Acquire a token
			var lease = await limiter.AcquireAsync(context, 1, context.RequestAborted);

			if (!lease.IsAcquired) {
				if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter)) {
					context.Response.Headers.RetryAfter =
						((int)retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);

					context.Response.Headers["X-RateLimit-Reset"] =
						DateTimeOffset.UtcNow.Add(retryAfter).ToUnixTimeSeconds().ToString();
				}

				context.Response.Headers["X-RateLimit-Limit"] = options.TokenLimit.ToString();
				context.Response.Headers["X-RateLimit-Remaining"] = "0";

				context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
				await context.Response.WriteAsync("Too Many Requests");
				return;
			}

			context.Response.OnStarting(() => {
				context.Response.Headers["X-RateLimit-Limit"] = options.TokenLimit.ToString();
				context.Response.Headers["X-RateLimit-Reset"] =
					DateTimeOffset.UtcNow.Add(TimeSpan.FromSeconds(options.ReplenishmentPeriod)).ToUnixTimeSeconds()
						.ToString();

				return Task.CompletedTask;
			});

			await next();
		});
	}
}
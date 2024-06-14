using System.Threading.RateLimiting;

namespace HypixelAPI;

public interface IHypixelRequestLimiter : IAsyncDisposable {
	void UpdateRequestLimit(int limit, int remaining = -1);
	ValueTask<RateLimitLease> AcquireAsync(int permitCount = 1);
	ValueTask<RateLimitLease> AcquireAsync(int permitCount, CancellationToken cancellationToken);
	ValueTask<RateLimitLease> AcquireOneAsync() => AcquireAsync(1);
	ValueTask<RateLimitLease> AcquireOneAsync(CancellationToken cancellationToken) => AcquireAsync(1, cancellationToken);
}

public class HypixelRequestLimiter : IHypixelRequestLimiter {
	
	/// <summary>
	/// Default options for the rate limiter, adjusted automatically to Hypixel's ratelimit headers on use.
	/// </summary>
	private static readonly SlidingWindowRateLimiterOptions LimiterOptions = new() {
		Window = TimeSpan.FromMinutes(5),
		SegmentsPerWindow = 5 * 4, // 15 seconds per segment
		AutoReplenishment = true,
		QueueLimit = 100,
		PermitLimit = 300, // Default Hypixel limit
		QueueProcessingOrder = QueueProcessingOrder.OldestFirst
	};

	private SlidingWindowRateLimiter Limiter { get; set; } = new(LimiterOptions);
	private int Remaining { get; set; } = LimiterOptions.PermitLimit;
	
	public void UpdateRequestLimit(int limit, int remaining = -1) {
		if (limit != LimiterOptions.PermitLimit) {
			LimiterOptions.PermitLimit = limit;
			Limiter = new SlidingWindowRateLimiter(LimiterOptions);
		}
		
		if (remaining == -1) return;

		var available = (int?) Limiter.GetStatistics()?.CurrentAvailablePermits ?? Remaining;
		
		if (remaining < available) {
			Remaining = remaining;
			Limiter.AttemptAcquire(Math.Max(available - remaining, 0));
		}
	}
	
	public ValueTask<RateLimitLease> AcquireAsync(int permitCount = 1) {
		Remaining -= permitCount;
		return Limiter.AcquireAsync(permitCount);
	}

	public ValueTask<RateLimitLease> AcquireAsync(int permitCount, CancellationToken cancellationToken) {
		Remaining -= permitCount;
		return Limiter.AcquireAsync(permitCount, cancellationToken);
	}
	
	async ValueTask IAsyncDisposable.DisposeAsync()
	{
		await Limiter.DisposeAsync().ConfigureAwait(false);
		GC.SuppressFinalize(this);
	}
}
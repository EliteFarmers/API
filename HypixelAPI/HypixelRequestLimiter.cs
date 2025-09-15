using System.Threading.RateLimiting;
using Microsoft.Extensions.Logging;

namespace EliteFarmers.HypixelAPI;

public interface IHypixelRequestLimiter {
	void Sync(int serverLimit, int serverRemaining);
	ValueTask<RateLimitLease> AcquireAsync(int permitCount, CancellationToken cancellationToken = default);
	ValueTask<RateLimitLease> AcquireOneAsync() => AcquireAsync(1);
	ValueTask<RateLimitLease> AcquireOneAsync(CancellationToken cancellationToken) => AcquireAsync(1, cancellationToken);
}

public class HypixelRequestLimiter(ILogger<IHypixelRequestLimiter> logger) : IHypixelRequestLimiter 
{
	private volatile SlidingWindowRateLimiter? _limiter;
	private int _configuredPermitLimit = -1;
	
#if NET9_0_OR_GREATER
	private readonly Lock _lock = new();
#else
	private readonly object _lock = new();
#endif
	
	private readonly RateLimiter _bootstrapLimiter = new FixedWindowRateLimiter(
		new FixedWindowRateLimiterOptions
		{
			PermitLimit = 10, 
			Window = TimeSpan.FromSeconds(3)
		});
	
    /// <summary>
    /// Ensures the limiter is configured correctly based on server headers.
    /// This method is thread-safe.
    /// </summary>
    public void Sync(int serverLimit, int serverRemaining)
    {
        // Check if we need to create or replace the limiter.
        if (_limiter == null || _configuredPermitLimit != serverLimit)
        {
            // If the limiter needs to be created or replaced, acquire a lock.
            lock (_lock)
            {
                // Check again inside the lock to handle race conditions
                if (_limiter == null || _configuredPermitLimit != serverLimit)
                {
                    // Create a new SlidingWindowRateLimiter with the server's limit.
                    var newLimiter = new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = serverLimit,
                        Window = TimeSpan.FromMinutes(5),
                        SegmentsPerWindow = 20,
                        AutoReplenishment = true,
                        QueueLimit = 100,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
                    
                    logger.LogWarning("Hypixel API rate limit changed/initialized! New limit: {ServerLimit}, Remaining: {ServerRemaining}", serverLimit, serverRemaining);

                    // Immediately sync the new limiter's remaining count.
                    var permitsToBurn = serverLimit - serverRemaining;
                    if (permitsToBurn > 0)
                    {
                        newLimiter.AttemptAcquire(permitsToBurn);
                    }
                    
                    _configuredPermitLimit = serverLimit;
                    _limiter = newLimiter;
                    return;
                }
            }
        }

        // Limiter is already configured, so we just need to sync the remaining permits.
        var availableLocally = _limiter.GetStatistics()?.CurrentAvailablePermits ?? 0;
        if (serverRemaining < availableLocally)
        {
            var permitsToBurn = availableLocally - serverRemaining;
            _limiter.AttemptAcquire((int) permitsToBurn);
        }
    }
	
	public async ValueTask<RateLimitLease> AcquireAsync(int permitCount = 1, CancellationToken cancellationToken = default) {
		// Allow request through bootstrap limiter if the main limiter is not initialized yet.
		if (_limiter == null)
		{
			return await _bootstrapLimiter.AcquireAsync(permitCount, cancellationToken);
		}

		return await _limiter.AcquireAsync(permitCount, cancellationToken);
	}
}
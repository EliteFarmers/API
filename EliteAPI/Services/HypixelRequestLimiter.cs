using System.Threading.RateLimiting;

namespace EliteAPI.Services; 

public class HypixelRequestLimiter {
    private readonly TokenBucketRateLimiter _limiter;
    private int _requestsPerMinute = 60;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HypixelRequestLimiter> _logger;

    public HypixelRequestLimiter(ILogger<HypixelRequestLimiter> logger, IConfiguration configuration) {
        _configuration = configuration;
        _logger = logger;
        
        GetRequestLimit();
        
        var tokensPerBucket = (int) Math.Floor(_requestsPerMinute / 6f);
        
        _limiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions {
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = tokensPerBucket,
            TokenLimit = tokensPerBucket
        });
    }
    
    public async Task AcquireAsync(int amount = 1) {
        await _limiter.AcquireAsync(amount);
    }
    
    private void GetRequestLimit()
    {
        try
        {
            _requestsPerMinute = _configuration.GetValue<int>("HypixelRequestLimit");
            _logger.LogWarning("HypixelRequestLimit set to {Requests}", _requestsPerMinute);
        }
        catch (Exception)
        {
            _logger.LogError("HypixelRequestLimit variable is not a valid number, defaulting to 60");
            _requestsPerMinute = 60;
        }
    }
}

using EliteAPI.Models.DTOs.Incoming;
using Microsoft.AspNetCore.Mvc;
using System.Threading.RateLimiting;

namespace EliteAPI.Services.HypixelService;

public class HypixelService : IHypixelService
{
    public static readonly string HttpClientName = "EliteDev";
    private readonly string _hypixelApiKey = Environment.GetEnvironmentVariable("HYPIXEL_API_KEY") 
                                             ?? throw new Exception("HYPIXEL_API_KEY env variable is not set.");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RateLimiter _rateLimiter;
    private int _requestsPerMinute;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HypixelService> _logger;

    public HypixelService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HypixelService> logger)
    {
        GetRequestLimit();
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;

        var tokensPerBucket = (int) Math.Floor(_requestsPerMinute / 6f);
        _rateLimiter = new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = tokensPerBucket,
            TokenLimit = tokensPerBucket
        });
    }

    public async Task<ActionResult<RawProfilesResponse>> FetchProfiles(string uuid) 
    {
        await _rateLimiter.AcquireAsync(1);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("API-Key", _hypixelApiKey);
        
        try
        {
            var data = await client.GetFromJsonAsync<RawProfilesResponse>($"https://api.hypixel.net/skyblock/profiles?uuid={uuid}");
            
            if (data is not { Success: true })
            {
                return new NotFoundResult();
            }

            return data;
        } catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, e);
        }

        return new BadRequestResult();
    }

    public async Task<ActionResult<RawPlayerResponse>> FetchPlayer(string uuid)
    {
        await _rateLimiter.AcquireAsync(1);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("API-Key", _hypixelApiKey);
        
        try
        {
            var data = await client.GetFromJsonAsync<RawPlayerResponse>($"https://api.hypixel.net/player?uuid={uuid}");

            if (data is not { Success: true })
            {
                return new NotFoundResult();
            }

            return data;
        } catch (Exception e)
        {
            _logger.LogError(e, "Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, e);
        }

        return new BadRequestResult();
    }

    private void GetRequestLimit()
    {
        try
        {
            _requestsPerMinute = _configuration.GetValue<int>("HypixelRequestLimit");
        }
        catch (Exception)
        {
            Console.Error.WriteLine("HYPIXEL_REQUEST_LIMIT env variable is not a valid number, defaulting to 60.");
            _requestsPerMinute = 60;
        }
    }
}

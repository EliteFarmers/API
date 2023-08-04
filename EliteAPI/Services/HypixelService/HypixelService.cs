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
    private readonly ILogger<HypixelService> _logger;
    private readonly HypixelRequestLimiter _limiter;

    public HypixelService(IHttpClientFactory httpClientFactory, HypixelRequestLimiter limiter, ILogger<HypixelService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _limiter = limiter;
    }

    public async Task<ActionResult<RawProfilesResponse>> FetchProfiles(string uuid) 
    {
        await _limiter.AcquireAsync(1);

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
        await _limiter.AcquireAsync(1);

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
}

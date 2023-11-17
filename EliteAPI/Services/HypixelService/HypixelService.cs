using System.Net;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.AspNetCore.Mvc;

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
        if (uuid.Length is not (32 or 36)) return new BadRequestResult();
        await _limiter.AcquireAsync(1);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("API-Key", _hypixelApiKey);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hypixel.net/v2/skyblock/profiles?uuid={uuid}");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                response.Headers.TryGetValues("ratelimit-limit", out var limit);
                
                if (limit is not null) {
                    _logger.LogWarning("Hypixel API rate limit exceeded! Limit: {Limit}", limit);
                }
                else _logger.LogWarning("Hypixel API rate limit exceeded!");
            }
            else {
                _logger.LogError("Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, response.StatusCode);
            }
            
            return new BadRequestResult();
        }

        try
        {
            var data = await response.Content.ReadFromJsonAsync<RawProfilesResponse>();
            
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
        if (uuid.Length is not (32 or 36)) return new BadRequestResult();
        await _limiter.AcquireAsync(1);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("API-Key", _hypixelApiKey);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hypixel.net/v2/player?uuid={uuid}");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                response.Headers.TryGetValues("ratelimit-limit", out var limit);

                if (limit is not null) {
                    _logger.LogWarning("Hypixel API rate limit exceeded! Limit: {Limit}", limit);
                }
                else _logger.LogWarning("Hypixel API rate limit exceeded!");
            }
            else {
                _logger.LogError("Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, response.StatusCode);
            }
            
            return new BadRequestResult();
        }

        try
        {
            var data = await response.Content.ReadFromJsonAsync<RawPlayerResponse>();

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

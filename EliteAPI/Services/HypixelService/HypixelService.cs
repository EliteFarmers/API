using System.Net;
using EliteAPI.Models.DTOs.Incoming;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Services.HypixelService;

public class HypixelService(
    IHttpClientFactory httpClientFactory,
    HypixelRequestLimiter limiter,
    ILogger<HypixelService> logger)
    : IHypixelService 
{
    public static readonly string HttpClientName = "EliteDev";
    private readonly string _hypixelApiKey = Environment.GetEnvironmentVariable("HYPIXEL_API_KEY") 
                                             ?? throw new Exception("HYPIXEL_API_KEY env variable is not set.");

    public async Task<ActionResult<RawProfilesResponse>> FetchProfiles(string uuid) 
    {
        if (uuid.Length is not (32 or 36)) return new BadRequestResult();
        await limiter.AcquireAsync(1);

        var client = httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("API-Key", _hypixelApiKey);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hypixel.net/v2/skyblock/profiles?uuid={uuid}");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                response.Headers.TryGetValues("ratelimit-limit", out var limit);
                
                if (limit is not null) {
                    logger.LogWarning("Hypixel API rate limit exceeded! Limit: {Limit}", limit);
                }
                else logger.LogWarning("Hypixel API rate limit exceeded!");
            }
            else {
                logger.LogError("Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, response.StatusCode);
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
            logger.LogError(e, "Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, e);
        }

        return new BadRequestResult();
    }

    public async Task<ActionResult<RawPlayerResponse>> FetchPlayer(string uuid)
    {
        if (uuid.Length is not (32 or 36)) return new BadRequestResult();
        await limiter.AcquireAsync(1);

        var client = httpClientFactory.CreateClient(HttpClientName);
        client.DefaultRequestHeaders.Add("API-Key", _hypixelApiKey);

        var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.hypixel.net/v2/player?uuid={uuid}");
        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests) {
                response.Headers.TryGetValues("ratelimit-limit", out var limit);

                if (limit is not null) {
                    logger.LogWarning("Hypixel API rate limit exceeded! Limit: {Limit}", limit);
                }
                else logger.LogWarning("Hypixel API rate limit exceeded!");
            }
            else {
                logger.LogError("Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, response.StatusCode);
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
            logger.LogError(e, "Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, e);
        }

        return new BadRequestResult();
    }
}

using System.Net;
using EliteAPI.Services.Interfaces;
using HypixelAPI;
using HypixelAPI.DTOs;
using Microsoft.AspNetCore.Mvc;
using Refit;

namespace EliteAPI.Services;

public class HypixelService(
    IHypixelApi hypixelApi,
    ILogger<HypixelService> logger)
    : IHypixelService 
{
    public static readonly string HttpClientName = "EliteDev";
    private readonly string _hypixelApiKey = Environment.GetEnvironmentVariable("HYPIXEL_API_KEY") 
                                             ?? throw new Exception("HYPIXEL_API_KEY env variable is not set.");

    public async Task<ActionResult<ProfilesResponse>> FetchProfiles(string uuid) 
    {
        if (uuid.Length is not (32 or 36)) return new BadRequestResult();
        var response = await hypixelApi.FetchProfiles(uuid);
        
        if (!response.IsSuccessStatusCode)
        {
            LogRateLimitWarnings(response);
            logger.LogError("Failed to fetch profiles for {Uuid}, Error: {Error}", uuid, response.StatusCode);
            return new BadRequestResult();
        }

        if (response.Content is not { Success: true })
        {
            return new BadRequestResult();
        }

        return response.Content;
    }

    public async Task<ActionResult<PlayerResponse>> FetchPlayer(string uuid)
    {
        if (uuid.Length is not (32 or 36)) return new BadRequestResult();
        var response = await hypixelApi.FetchPlayer(uuid);

        if (!response.IsSuccessStatusCode)
        {
            LogRateLimitWarnings(response);
            logger.LogError("Failed to fetch player for {Uuid}, Error: {Error}", uuid, response.StatusCode);
            return new BadRequestResult();
        }
        
        if (response.Content is not { Success: true })
        {
            return new BadRequestResult();
        }

        return response.Content;
    }
    
    public async Task<ActionResult<GardenResponse>> FetchGarden(string profileId)
    {
        if (profileId.Length is not (32 or 36)) return new BadRequestResult();
        var response = await hypixelApi.FetchGarden(profileId);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NotFound) return new NotFoundResult();
            LogRateLimitWarnings(response);
            
            logger.LogError("Failed to fetch garden for {Uuid}, Error: {Error}", profileId, response.StatusCode);
            
            return new BadRequestResult();
        }
        
        if (response.Content is not { Success: true })
        {
            return new BadRequestResult();
        }

        return response.Content;
    }
    
    private void LogRateLimitWarnings<T>(ApiResponse<T> response) {
        if (response.StatusCode != HttpStatusCode.TooManyRequests) return;
        
        response.Headers.TryGetValues("ratelimit-limit", out var limit);

        if (limit is not null) {
            logger.LogWarning("Hypixel API rate limit exceeded! Limit: {Limit}", limit);
        }
        else logger.LogWarning("Hypixel API rate limit exceeded!");
    }
}

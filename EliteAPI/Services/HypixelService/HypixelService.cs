using Discord;
using EliteAPI.Data.Models.Hypixel;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using System.Threading.RateLimiting;

namespace EliteAPI.Services.HypixelService;

public class HypixelService : IHypixelService
{
    public static readonly string HttpClientName = "EliteDev";
    private readonly string HypixelAPIKey = Environment.GetEnvironmentVariable("HYPIXEL_API_KEY") ?? throw new Exception("HYPIXEL_API_KEY env variable is not set.");
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly RateLimiter RateLimiter;
    private int RequestsPerMinute;


    public HypixelService(IHttpClientFactory httpClientFactory)
    {
        GetRequestLimit();
        _httpClientFactory = httpClientFactory;

        var tokensPerBucket = (int) Math.Floor(RequestsPerMinute / 6f);
        RateLimiter = new TokenBucketRateLimiter(new()
        {
            ReplenishmentPeriod = TimeSpan.FromSeconds(10),
            TokensPerPeriod = tokensPerBucket,
            TokenLimit = tokensPerBucket
        });
    }

    public Task<ActionResult> FetchPlayer(string uuid)
    {
        throw new NotImplementedException();
    }

    public async Task<ActionResult<RawProfilesResponse>> FetchProfiles(string uuid) 
    {
        Console.WriteLine("Fetching profiles");
        await RateLimiter.AcquireAsync(1);

        var client = _httpClientFactory.CreateClient(HttpClientName);
        
        try
        {
            var data = await client.GetFromJsonAsync<RawProfilesResponse>($"https://api.hypixel.net/skyblock/profiles?uuid={uuid}&key={HypixelAPIKey}");

            if (data == null || !data.Success)
            {
                return new NotFoundResult();
            }

            return data;
        } catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return new BadRequestResult();
    }

    private void GetRequestLimit()
    {
        var limit = Environment.GetEnvironmentVariable("HYPIXEL_REQUEST_LIMIT") ?? "60";
        try
        {
            RequestsPerMinute = int.Parse(limit);
        }
        catch (Exception)
        {
            Console.Error.WriteLine("HYPIXEL_REQUEST_LIMIT env variable is not a valid number, defaulting to 60.");
            RequestsPerMinute = 60;
        }
    }
}

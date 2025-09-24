using EliteAPI.Data;
using EliteAPI.Features.Resources.Firesales.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using SkyblockRepo;
using SkyblockRepo.Models.Misc;

namespace EliteAPI.Features.Resources.Firesales.Endpoints;

internal sealed class SkyblockGemShopEndpoint(
    DataContext context
) : EndpointWithoutRequest<SkyblockGemShopsResponse> {
	
    public override void Configure() {
        Get("/resources/gems");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "Get Skyblock Gem Shops";
            s.Description = "Get the current/upcoming Skyblock firesales, Taylor's Collection, and Seasonal Bundles.";
        });
		
        Options(o => {
            o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("gemshops"));
        });
    }

    public override async Task HandleAsync(CancellationToken c) {
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var result = await context.SkyblockFiresales
            .Where(s => s.EndsAt >= currentTime)
            .SelectDto()
            .ToListAsync(cancellationToken: c);
		
        await Send.OkAsync(new SkyblockGemShopsResponse()
        {
            Firesales = result,
            TaylorCollection = SkyblockRepoClient.Data.TaylorCollection,
            SeasonalBundles = SkyblockRepoClient.Data.SeasonalBundles
        }, cancellation: c);
    }
}

internal sealed class SkyblockGemShopsResponse
{
    public List<SkyblockFiresaleDto> Firesales { get; set; } = [];
    public TaylorCollection TaylorCollection { get; set; } = new();
    public TaylorCollection SeasonalBundles { get; set; } = new();
}
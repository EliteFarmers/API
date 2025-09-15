using EliteAPI.Data;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Items.Endpoints;

internal sealed class GetSkyblockItemsEndpoint(
    DataContext context
) : EndpointWithoutRequest<GetSkyblockItemsResponse> {
	
    public override void Configure() {
        Get("/resources/items");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "Get Skyblock Items";
            s.Description = "Get all items in the Hypixel resources endpoint";
        });
		
        Options(o => {
            o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("items"));
        });
    }

    public override async Task HandleAsync(CancellationToken c) {
        var result = await context.SkyblockItems
            .Select(s => new { s.ItemId, s.Data })
            .Where(s => s.Data != null)
            .ToDictionaryAsync(k => k.ItemId, v => v.Data, cancellationToken: c);
		
        await Send.OkAsync(new GetSkyblockItemsResponse { Items = result }, cancellation: c);
    }
}

internal sealed class GetSkyblockItemsResponse
{
    public Dictionary<string, ItemResponse?> Items { get; set; } = new();
}
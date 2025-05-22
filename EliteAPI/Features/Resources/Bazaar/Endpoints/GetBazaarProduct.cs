using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar.Endpoints;

internal sealed class GetBazaarProductRequest
{
    public required string ItemId { get; set; }
}

internal sealed class GetBazaarProductEndpoint(
    DataContext context
) : Endpoint<GetBazaarProductRequest, GetBazaarProductResponse> {
	
    public override void Configure() {
        Get("/resources/bazaar/{ItemId}");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "Get Bazaar Product";
        });
		
        Options(o => {
            o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("bazaar"));
        });
    }

    public override async Task HandleAsync(GetBazaarProductRequest request, CancellationToken c) {
        var result = await context.BazaarProductSummaries
            .Include(s => s.SkyblockItem)
            .Select(s => new GetBazaarProductResponse() {
                ProductId = s.ItemId,
                Product = new BazaarProductSummaryDto()
                {
                    Npc = s.SkyblockItem.NpcSellPrice,
                    Sell = s.InstaSellPrice,
                    Buy = s.InstaBuyPrice,
                    SellOrder = s.SellOrderPrice,
                    BuyOrder = s.BuyOrderPrice,
                    AverageSell = s.AvgInstaSellPrice,
                    AverageBuy = s.AvgInstaBuyPrice,
                    AverageBuyOrder = s.AvgBuyOrderPrice,
                    AverageSellOrder = s.AvgSellOrderPrice,
                }})
            .Where(s => s.ProductId == request.ItemId)
            .FirstOrDefaultAsync(cancellationToken: c);

        if (result is null)
        {
            await SendNotFoundAsync(c);
            return;
        }
		
        await SendAsync(result, cancellation: c);
    }
}

internal sealed class GetBazaarProductResponse
{
    public string ProductId { get; set; }
    public BazaarProductSummaryDto Product { get; set; } = new();
}
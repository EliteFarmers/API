using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar.Endpoints;

internal sealed class BazaarOverviewResponse
{
    public List<BazaarOverviewItemDto> TopMovers { get; set; } = [];
    public List<BazaarOverviewItemDto> MostTraded { get; set; } = [];
}

internal sealed class BazaarOverviewItemDto
{
    public required string ItemId { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public BazaarProductSummaryDto Summary { get; set; } = new();
}

internal sealed class GetBazaarOverviewEndpoint(
    DataContext context
) : EndpointWithoutRequest<BazaarOverviewResponse>
{
    public override void Configure()
    {
        Get("/resources/bazaar/overview");
        AllowAnonymous();
        Version(0);
        
        Summary(s => {
            s.Summary = "Get Bazaar Overview";
            s.Description = "Get overview of bazaar with top movers and most traded items.";
        });
        
        Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(5)).Tag("bazaar")); });
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var allProducts = await context.BazaarProductSummaries
            .Include(s => s.SkyblockItem)
            .AsNoTracking()
            .ToListAsync(c);

        // Top movers - highest price change percentage
        var topMovers = allProducts
            .Where(s => s.AvgInstaBuyPrice > 0 && s.InstaBuyPrice > 0)
            .OrderByDescending(s => Math.Abs(s.InstaBuyPrice - s.AvgInstaBuyPrice) / s.AvgInstaBuyPrice)
            .Take(10)
            .Select(s => new BazaarOverviewItemDto
            {
                ItemId = s.ItemId,
                Name = s.SkyblockItem.Data?.Name,
                Category = s.SkyblockItem.Data?.Category,
                Summary = new BazaarProductSummaryDto
                {
                    Name = s.SkyblockItem.Data?.Name,
                    Npc = s.SkyblockItem.NpcSellPrice,
                    Sell = s.InstaSellPrice,
                    Buy = s.InstaBuyPrice,
                    SellOrder = s.SellOrderPrice,
                    BuyOrder = s.BuyOrderPrice,
                    AverageSell = s.AvgInstaSellPrice,
                    AverageBuy = s.AvgInstaBuyPrice,
                    AverageBuyOrder = s.AvgBuyOrderPrice,
                    AverageSellOrder = s.AvgSellOrderPrice
                }
            })
            .ToList();

        // Most traded - highest buy+sell price (as proxy for volume)
        var mostTraded = allProducts
            .OrderByDescending(s => s.InstaBuyPrice + s.InstaSellPrice)
            .Take(10)
            .Select(s => new BazaarOverviewItemDto
            {
                ItemId = s.ItemId,
                Name = s.SkyblockItem.Data?.Name,
                Category = s.SkyblockItem.Data?.Category,
                Summary = new BazaarProductSummaryDto
                {
                    Name = s.SkyblockItem.Data?.Name,
                    Npc = s.SkyblockItem.NpcSellPrice,
                    Sell = s.InstaSellPrice,
                    Buy = s.InstaBuyPrice,
                    SellOrder = s.SellOrderPrice,
                    BuyOrder = s.BuyOrderPrice,
                    AverageSell = s.AvgInstaSellPrice,
                    AverageBuy = s.AvgInstaBuyPrice,
                    AverageBuyOrder = s.AvgBuyOrderPrice,
                    AverageSellOrder = s.AvgSellOrderPrice
                }
            })
            .ToList();

        await Send.OkAsync(new BazaarOverviewResponse
        {
            TopMovers = topMovers,
            MostTraded = mostTraded
        }, c);
    }
}

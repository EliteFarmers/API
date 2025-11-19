using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Items.Endpoints;

internal sealed class ItemRelatedRequest
{
    public required string SkyblockId { get; set; }
}

internal sealed class ItemRelatedResponse
{
    public List<ItemResponse> Similar { get; set; } = [];
    public ItemTrendDto? Trends { get; set; }
}

internal sealed class ItemTrendDto
{
    public double PriceChangePercentage { get; set; }
    public double VolumeChangePercentage { get; set; }
}

internal sealed class GetItemRelatedEndpoint(
    DataContext context
) : Endpoint<ItemRelatedRequest, ItemRelatedResponse>
{
    public override void Configure()
    {
        Get("/resources/items/{SkyblockId}/related");
        AllowAnonymous();
        Version(0);
        
        Summary(s => {
            s.Summary = "Get Related Items and Trends";
            s.Description = "Get similar items and trend data for a specific item.";
        });
        
        ResponseCache(300);
    }

    public override async Task HandleAsync(ItemRelatedRequest req, CancellationToken c)
    {
        var targetItem = await context.SkyblockItems
            .AsNoTracking()
            .Where(i => i.ItemId == req.SkyblockId)
            .Select(i => new { i.Data })
            .FirstOrDefaultAsync(c);

        if (targetItem?.Data == null)
        {
            await Send.NotFoundAsync(c);
            return;
        }

        var category = targetItem.Data.Category;

        var similar = await context.SkyblockItems
            .AsNoTracking()
            .Where(i => i.Data != null && i.Data.Category == category && i.ItemId != req.SkyblockId)
            .Take(10)
            .Select(i => i.Data!)
            .ToListAsync(c);

        var now = DateTimeOffset.UtcNow;
        var oneDayAgo = now.AddDays(-1);
        
        var history = await context.AuctionPriceHistories
            .AsNoTracking()
            .Where(h => h.SkyblockId == req.SkyblockId && h.CalculatedAt >= oneDayAgo.AddHours(-2))
            .OrderByDescending(h => h.CalculatedAt)
            .ToListAsync(c);

        ItemTrendDto? trends = null;
        
        if (history.Count >= 2)
        {
            var latest = history.First();
            var targetTime = latest.CalculatedAt.AddDays(-1);
            var past = history.MinBy(h => Math.Abs((h.CalculatedAt - targetTime).TotalMinutes));

            if (past != null && past != latest && latest.AverageSalePrice.HasValue && past.AverageSalePrice.HasValue && past.AverageSalePrice.Value != 0)
            {
                var priceChange = (double)((latest.AverageSalePrice.Value - past.AverageSalePrice.Value) / past.AverageSalePrice.Value) * 100;
                var volumeChange = past.ItemsSold != 0 
                    ? (double)(latest.ItemsSold - past.ItemsSold) / past.ItemsSold * 100 
                    : 0;

                trends = new ItemTrendDto
                {
                    PriceChangePercentage = priceChange,
                    VolumeChangePercentage = volumeChange
                };
            }
        }

        await Send.OkAsync(new ItemRelatedResponse
        {
            Similar = similar,
            Trends = trends
        }, c);
    }
}

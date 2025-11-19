using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar.Endpoints;

internal sealed class SearchBazaarProductsRequest
{
    [QueryParam]
    public string? Query { get; set; }
    
    [QueryParam]
    public string? Category { get; set; }
    
    [QueryParam]
    public int Limit { get; set; } = 50;
}

internal sealed class SearchBazaarProductsResponse
{
    public List<BazaarSearchResultDto> Products { get; set; } = [];
}

internal sealed class BazaarSearchResultDto
{
    public required string ItemId { get; set; }
    public string? Name { get; set; }
    public string? Category { get; set; }
    public BazaarProductSummaryDto Summary { get; set; } = new();
}

internal sealed class SearchBazaarProductsEndpoint(
    DataContext context
) : Endpoint<SearchBazaarProductsRequest, SearchBazaarProductsResponse>
{
    public override void Configure()
    {
        Get("/resources/bazaar/search");
        AllowAnonymous();
        Version(0);
        
        Summary(s => {
            s.Summary = "Search Bazaar Products";
            s.Description = "Search bazaar products by name or category. Returns all products if no filters provided.";
        });
        
        Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("bazaar")); });
    }

    public override async Task HandleAsync(SearchBazaarProductsRequest req, CancellationToken c)
    {
        var query = context.BazaarProductSummaries
            .Include(s => s.SkyblockItem)
            .AsNoTracking();

        // Filter by category
        if (!string.IsNullOrWhiteSpace(req.Category))
        {
            query = query.Where(s => s.SkyblockItem.Data != null && 
                                    s.SkyblockItem.Data.Category != null &&
                                    s.SkyblockItem.Data.Category.ToLower().Contains(req.Category.ToLower()));
        }

        // Filter by search query
        if (!string.IsNullOrWhiteSpace(req.Query))
        {
            var searchTerm = req.Query.ToLower();
            query = query.Where(s => 
                (s.SkyblockItem.Data != null && s.SkyblockItem.Data.Name != null && s.SkyblockItem.Data.Name.ToLower().Contains(searchTerm)) ||
                s.ItemId.ToLower().Contains(searchTerm)
            );
        }

        var results = await query
            .OrderByDescending(s => s.AvgInstaBuyPrice > 0 ? s.AvgInstaBuyPrice : s.InstaBuyPrice)
            .Take(Math.Clamp(req.Limit, 1, 100))
            .Select(s => new BazaarSearchResultDto
            {
                ItemId = s.ItemId,
                Name = s.SkyblockItem.Data != null ? s.SkyblockItem.Data.Name : null,
                Category = s.SkyblockItem.Data != null ? s.SkyblockItem.Data.Category : null,
                Summary = new BazaarProductSummaryDto
                {
                    Name = s.SkyblockItem.Data != null ? s.SkyblockItem.Data.Name : null,
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
            .ToListAsync(c);

        await Send.OkAsync(new SearchBazaarProductsResponse { Products = results }, c);
    }
}

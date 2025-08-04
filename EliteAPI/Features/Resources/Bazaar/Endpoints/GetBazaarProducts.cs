using System.Text.Json.Serialization;
using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar.Endpoints;

internal sealed class GetBazaarProductsEndpoint(
    DataContext context
) : EndpointWithoutRequest<GetBazaarProductsResponse> {
	
    public override void Configure() {
        Get("/resources/bazaar");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "Get Bazaar Products";
            s.Description = "Get all bazaar products.";
        });
		
        Options(o => {
            o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("bazaar"));
        });
    }

    public override async Task HandleAsync(CancellationToken c) {
        var result = await context.BazaarProductSummaries
            .Include(bazaarSummary => bazaarSummary.SkyblockItem)
            .Select(s => new {
                ProductId = s.ItemId, Info = new BazaarProductSummaryDto()
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
                AverageSellOrder = s.AvgSellOrderPrice,
            }})
            .ToDictionaryAsync(k => k.ProductId, v => v.Info, cancellationToken: c);
		
        await Send.OkAsync(new GetBazaarProductsResponse() { Products = result }, cancellation: c);
    }
}

internal sealed class GetBazaarProductsResponse
{
    public Dictionary<string, BazaarProductSummaryDto> Products { get; set; } = new();
}

internal sealed class BazaarProductSummaryDto
{
    /// <summary>
    /// Name of the item if it exists.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Name { get; set; }
    /// <summary>
    /// NPC sell price of the item if it exists.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Npc { get; set; }
    /// <summary>
    /// Instant Sell price taken directly from most recently fetched data
    /// </summary>
    public double Sell { get; set; }
    /// <summary>
    /// Instant Buy price taken directly from most recently fetched data
    /// </summary>
    public double Buy { get; set; }
    /// <summary>
    /// Sell Order price calculated from most recently fetched data
    /// </summary>
    public double SellOrder { get; set; }
    /// <summary>
    /// Buy Order price calculated from most recently fetched data
    /// </summary>
    public double BuyOrder { get; set; }
    /// <summary>
    /// Calculated average Instant Sell price that should be more resistant to price fluctuations
    /// </summary>
    public double AverageSell { get; set; }
    /// <summary>
    /// Calculated average Instant Buy price that should be more resistant to price fluctuations
    /// </summary>
    public double AverageBuy { get; set; }
    /// <summary>
    /// Calculated average Sell Order price that should be more resistant to price fluctuations
    /// </summary>
    public double AverageSellOrder { get; set; }
    /// <summary>
    /// Calculated average Buy Order price that should be more resistant to price fluctuations
    /// </summary>
    public double AverageBuyOrder { get; set; }
}
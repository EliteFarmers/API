using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;
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
		
        await SendAsync(new GetBazaarProductsResponse() { Products = result }, cancellation: c);
    }
}

internal sealed class GetBazaarProductsResponse
{
    public Dictionary<string, BazaarProductSummaryDto> Products { get; set; } = new();
}

internal sealed class BazaarProductSummaryDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Npc { get; set; }
    public double Sell { get; set; }
    public double Buy { get; set; }
    public double SellOrder { get; set; }
    public double BuyOrder { get; set; }
    public double AverageSell { get; set; }
    public double AverageBuy { get; set; }
    public double AverageSellOrder { get; set; }
    public double AverageBuyOrder { get; set; }
}
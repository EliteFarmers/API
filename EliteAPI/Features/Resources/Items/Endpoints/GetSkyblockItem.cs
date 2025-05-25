using System.Text.Json.Serialization;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Bazaar.Endpoints;
using FastEndpoints;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Items.Endpoints;

internal sealed class SkybProductRequest
{
    public required string ItemId { get; set; }
}

internal sealed class SkybProductEndpoint(
    DataContext context
) : Endpoint<SkybProductRequest, SkyblockItemResponse> {
	
    public override void Configure() {
        Get("/resources/items/{ItemId}");
        AllowAnonymous();
        Version(0);

        Summary(s => {
            s.Summary = "Get Skyblock Item";
            s.Description = "Get the Hypixel provided data of a specific item, as well as a bazaar summary.";
        });
		
        Options(o => {
            o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("items"));
        });
    }

    public override async Task HandleAsync(SkybProductRequest request, CancellationToken c) {
        var result = await context.SkyblockItems
            .Include(s => s.BazaarProductSummary)
            .Select(s => new SkyblockItemResponse() {
                ItemId = s.ItemId,
                Data = s.Data,
                Name = s.Data != null ? s.Data.Name : null,
                Bazaar = s.BazaarProductSummary != null ? new BazaarProductSummaryDto()
                {
                    Npc = s.NpcSellPrice,
                    Sell = s.BazaarProductSummary.InstaSellPrice,
                    Buy = s.BazaarProductSummary.InstaBuyPrice,
                    SellOrder = s.BazaarProductSummary.SellOrderPrice,
                    BuyOrder = s.BazaarProductSummary.BuyOrderPrice,
                    AverageSell = s.BazaarProductSummary.AvgInstaSellPrice,
                    AverageBuy = s.BazaarProductSummary.AvgInstaBuyPrice,
                    AverageBuyOrder = s.BazaarProductSummary.AvgBuyOrderPrice,
                    AverageSellOrder = s.BazaarProductSummary.AvgSellOrderPrice,
                } : null})
            .Where(s => s.ItemId == request.ItemId)
            .FirstOrDefaultAsync(cancellationToken: c);

        if (result is null)
        {
            await SendNotFoundAsync(c);
            return;
        }
		
        await SendAsync(result, cancellation: c);
    }
}

internal sealed class SkyblockItemResponse
{
    public required string ItemId { get; set; }
    public string? Name { get; set; }

    /// <summary>
    /// Data from the Hypixel items endpoint
    /// </summary>
    public ItemResponse? Data { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public BazaarProductSummaryDto? Bazaar { get; set; }
}
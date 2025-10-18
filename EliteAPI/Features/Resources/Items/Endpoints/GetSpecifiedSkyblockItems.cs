using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.DTOs;
using EliteAPI.Features.Resources.Bazaar.Endpoints;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Items.Endpoints;

internal sealed class GetSpecifiedSkyblockItemsRequest
{
	public List<string> Items { get; set; } = [];
}

internal sealed class GetSpecifiedSkyblockItemsEndpoint(
	DataContext context
) : Endpoint<GetSpecifiedSkyblockItemsRequest, GetSpecifiedSkyblockItemsResponse>
{
	public override void Configure() {
		Post("/resources/items");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Specific Skyblock Items";
			s.Description =
				"Get specific skyblock items from the Hypixel resources endpoint, along with bazaar data for each.";
			s.ExampleRequest = new GetSpecifiedSkyblockItemsRequest {
				Items = ["NETHER_STALK", "ENCHANTED_NETHER_STALK", "MUTANT_NETHER_STALK"]
			};
		});
	}

	public override async Task HandleAsync(GetSpecifiedSkyblockItemsRequest request, CancellationToken c) {
		var result = await context.SkyblockItems
			.Include(s => s.BazaarProductSummary)
			.Include(s => s.AuctionItems)
			.Select(s => new SkyblockItemResponse {
				ItemId = s.ItemId,
				Name = s.Data != null ? s.Data.Name : null,
				Data = s.Data,
				Auctions = s.AuctionItems != null ? s.AuctionItems.Select(a => a.ToDto()).ToList() : null,
				Bazaar = s.BazaarProductSummary != null
					? new BazaarProductSummaryDto {
						Npc = s.NpcSellPrice,
						Sell = s.BazaarProductSummary.InstaSellPrice,
						Buy = s.BazaarProductSummary.InstaBuyPrice,
						SellOrder = s.BazaarProductSummary.SellOrderPrice,
						BuyOrder = s.BazaarProductSummary.BuyOrderPrice,
						AverageSell = s.BazaarProductSummary.AvgInstaSellPrice,
						AverageBuy = s.BazaarProductSummary.AvgInstaBuyPrice,
						AverageBuyOrder = s.BazaarProductSummary.AvgBuyOrderPrice,
						AverageSellOrder = s.BazaarProductSummary.AvgSellOrderPrice
					}
					: null
			})
			.Where(s => request.Items.Contains(s.ItemId))
			.ToDictionaryAsync(k => k.ItemId, v => v, c);

		await Send.OkAsync(new GetSpecifiedSkyblockItemsResponse { Items = result }, c);
	}
}

internal sealed class GetSpecifiedSkyblockItemsResponse
{
	public Dictionary<string, SkyblockItemResponse> Items { get; set; } = new();
}

internal sealed class GetSpecifiedSkyblockItemsRequestValidator : Validator<GetSpecifiedSkyblockItemsRequest>
{
	public GetSpecifiedSkyblockItemsRequestValidator() {
		RuleFor(x => x.Items)
			.Must(list => list.Count is > 0 and <= 100)
			.WithMessage("Items array must contain at least one item and a maximum of 100");
	}
}
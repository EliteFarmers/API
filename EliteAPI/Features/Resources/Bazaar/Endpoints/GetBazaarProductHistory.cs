using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar.Endpoints;

internal sealed class GetBazaarProductHistoryRequest
{
	public required string ItemId { get; set; }
}

internal sealed class GetBazaarProductHistoryEndpoint(
	DataContext context
) : Endpoint<GetBazaarProductHistoryRequest, GetBazaarProductHistoryResponse>
{
	public override void Configure() {
		Get("/resources/bazaar/{ItemId}/history");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Bazaar Product History";
			s.Description = "Get a specific bazaar product's history";
		});

		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(2)).Tag("bazaar")); });
	}

	public override async Task HandleAsync(GetBazaarProductHistoryRequest request, CancellationToken c) {
		var result = await context.BazaarProductSummaries
			.Include(s => s.SkyblockItem)
			.Select(s => new GetBazaarProductHistoryResponse {
				ProductId = s.ItemId,
				Product = new BazaarProductSummaryDto {
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
			.Where(s => s.ProductId == request.ItemId)
			.FirstOrDefaultAsync(c);

		if (result is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		await Send.OkAsync(result, c);
	}
}

internal sealed class GetBazaarProductHistoryResponse
{
	public required string ProductId { get; set; }
	public BazaarProductSummaryDto Product { get; set; } = new();
}
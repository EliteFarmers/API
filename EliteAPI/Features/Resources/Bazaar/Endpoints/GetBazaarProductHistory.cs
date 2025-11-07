using EliteAPI.Data;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar.Endpoints;

internal sealed class GetBazaarProductHistoryRequest
{
	public required string ItemId { get; set; }
	public string? Timespan { get; set; }
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
		var timespan = request.Timespan ?? "7d";
		var timespanDays = timespan switch {
			"1d" => 1,
			"3d" => 3,
			"7d" => 7,
			"14d" => 14,
			"30d" => 30,
			_ => 7
		};

		var cutoff = DateTimeOffset.UtcNow.AddDays(-timespanDays);

		var summary = await context.BazaarProductSummaries
			.Include(s => s.SkyblockItem)
			.Where(s => s.ItemId == request.ItemId)
			.FirstOrDefaultAsync(c);

		if (summary is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var history = await context.BazaarProductSnapshots
			.AsNoTracking()
			.Where(s => s.ProductId == request.ItemId && s.RecordedAt >= cutoff)
			.OrderBy(s => s.RecordedAt)
			.Select(s => new BazaarHistoryDataPoint {
				Timestamp = s.RecordedAt,
				InstaSellPrice = s.InstaSellPrice,
				InstaBuyPrice = s.InstaBuyPrice,
				BuyOrderPrice = s.BuyOrderPrice,
				SellOrderPrice = s.SellOrderPrice
			})
			.ToListAsync(c);

		var response = new GetBazaarProductHistoryResponse {
			ProductId = summary.ItemId,
			Product = new BazaarProductSummaryDto {
				Name = summary.SkyblockItem.Data != null ? summary.SkyblockItem.Data.Name : null,
				Npc = summary.SkyblockItem.NpcSellPrice,
				Sell = summary.InstaSellPrice,
				Buy = summary.InstaBuyPrice,
				SellOrder = summary.SellOrderPrice,
				BuyOrder = summary.BuyOrderPrice,
				AverageSell = summary.AvgInstaSellPrice,
				AverageBuy = summary.AvgInstaBuyPrice,
				AverageBuyOrder = summary.AvgBuyOrderPrice,
				AverageSellOrder = summary.AvgSellOrderPrice
			},
			History = history
		};

		await Send.OkAsync(response, c);
	}
}

internal sealed class GetBazaarProductHistoryResponse
{
	public required string ProductId { get; set; }
	public BazaarProductSummaryDto Product { get; set; } = new();
	public List<BazaarHistoryDataPoint> History { get; set; } = new();
}

internal sealed class BazaarHistoryDataPoint
{
	public DateTimeOffset Timestamp { get; set; }
	public double InstaSellPrice { get; set; }
	public double InstaBuyPrice { get; set; }
	public double BuyOrderPrice { get; set; }
	public double SellOrderPrice { get; set; }
}
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZLinq;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseProductsEndpoint(
	DataContext context,
	IOptions<AuctionHouseSettings> auctionHouseSettings
) : EndpointWithoutRequest<AuctionHouseDto>
{
	private readonly AuctionHouseSettings _config = auctionHouseSettings.Value;

	public override void Configure() {
		Get("/resources/auctions");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Auction House";
			s.Description = "Get lowest auction house prices.";
		});

		ResponseCache(300);
		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromMinutes(10)).Tag("auctions")); });
	}

	public override async Task HandleAsync(CancellationToken c) {
		var data = await context.AuctionItems.AsNoTracking()
			.Where(r => r.CalculatedAt >= DateTime.UtcNow.AddDays(-_config.AggregationMaxLookbackDays))
			.GroupBy(a => a.SkyblockId)
			.ToListAsync(c);

		var response = new AuctionHouseDto {
			Items = data.AsValueEnumerable().ToDictionary(
				g => g.Key,
				g => g.Select(a => a.ToDto()).ToList()
			)
		};

		await Send.OkAsync(response, c);
	}
}

internal sealed class AuctionHouseDto
{
	public Dictionary<string, List<AuctionItemDto>> Items { get; set; } = [];
}
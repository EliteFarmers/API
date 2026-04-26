using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseNeuAverageLowestBinRequest
{
	/// <summary>
	/// Average lowest-BIN window. Supported values: 1day, 3day, 7day.
	/// </summary>
	public string? Window { get; set; }
}

internal sealed class GetAuctionHouseNeuAverageLowestBinEndpoint(AuctionHouseNeuService auctionHouseNeuService)
	: Endpoint<GetAuctionHouseNeuAverageLowestBinRequest, Dictionary<string, long>>
{
	public override void Configure() {
		Get("/resources/auctions/neu/average-lbin/{Window}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Average Lowest BIN Prices (NEU Format)";
			s.Description =
				"Get NEU lowest-BIN averages over a fixed history window using hourly lowest-BIN history buckets. " +
				"Supported windows: 1day, 3day, 7day.";
		});

		ResponseCache(60);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)).Tag("auctions"));
		});
	}

	public override async Task HandleAsync(GetAuctionHouseNeuAverageLowestBinRequest req, CancellationToken c) {
		var window = req.Window ?? Route<string>("Window");
		var windowDays = AuctionHouseNeuService.TryParseAverageLowestBinWindow(window);
		if (windowDays is null) {
			ThrowError("Unsupported average lowest-BIN window. Supported values: 1day, 3day, 7day.",
				400);
		}

		var result = await auctionHouseNeuService.BuildAverageLowestBinPricesAsync(windowDays.Value, c);
		await Send.OkAsync(result, c);
	}
}
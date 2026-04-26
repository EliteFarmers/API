using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseNeuAverageLowestBinGzipEndpoint(
	AuctionHouseNeuService auctionHouseNeuService,
	IOptions<JsonOptions> jsonOptions) : Endpoint<GetAuctionHouseNeuAverageLowestBinRequest>
{
	public override void Configure() {
		Get("/resources/auctions/neu/average-lbin/{Window}.gz");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Average Lowest BIN Prices (NEU Format, Gzip File)";
			s.Description =
				"Get the same average lowest-BIN NEU payload as /resources/auctions/neu/average-lbin/{window}. Only use this if you need to, the normal endpoint already supports gzip data transfer.";
		});

		ResponseCache(60);
		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)).Tag("auctions")); });
	}

	public override async Task HandleAsync(GetAuctionHouseNeuAverageLowestBinRequest req, CancellationToken c) {
		var window = req.Window ?? Route<string>("Window");
		var windowDays = AuctionHouseNeuService.TryParseAverageLowestBinWindow(window);
		if (windowDays is null) {
			ThrowError("Unsupported average lowest-BIN window. Supported values: 1day, 3day, 7day.", 400);
		}

		var result = await auctionHouseNeuService.BuildAverageLowestBinPricesAsync(windowDays.Value, c);
		var gzipBytes = await GetAuctionHouseNeuGzipEndpoint.CreateGzipPayloadAsync(result,
			jsonOptions.Value.SerializerOptions, c);

		await Send.BytesAsync(gzipBytes, contentType: "application/gzip", cancellation: c);
	}
}
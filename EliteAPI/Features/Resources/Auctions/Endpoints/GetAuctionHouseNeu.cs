using System.ComponentModel;
using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseNeuRequest
{
	/// <summary>
	/// Price mode: "raw" for absolute lowest BIN (like moulberry), "smooth" for IQR-filtered price. Default: raw.
	/// </summary>
	[QueryParam, DefaultValue("raw")]
	public string? Mode { get; set; }
}

internal sealed class GetAuctionHouseNeuEndpoint(AuctionHouseNeuService auctionHouseNeuService)
	: Endpoint<GetAuctionHouseNeuRequest, Dictionary<string, long>>
{
	public override void Configure() {
		Get("/resources/auctions/neu");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Lowest BIN Prices (NEU Format)";
			s.Description =
				"Get lowest BIN prices keyed by NEU internal names. Drop-in replacement for moulberry lowestbin.json. " +
				"Use ?mode=raw (default) for absolute cheapest BIN or ?mode=smooth for IQR-filtered prices.";
		});

		ResponseCache(60);
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromSeconds(60)).SetVaryByQuery("mode").Tag("auctions"));
		});
	}

	public override async Task HandleAsync(GetAuctionHouseNeuRequest req, CancellationToken c) {
		var result = await auctionHouseNeuService.BuildPricesAsync(req.Mode, c);
		await Send.OkAsync(result, c);
	}
}
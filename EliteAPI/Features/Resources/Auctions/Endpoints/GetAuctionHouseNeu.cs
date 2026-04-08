using System.ComponentModel;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Auctions.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

internal sealed class GetAuctionHouseNeuRequest
{
	/// <summary>
	/// Price mode: "raw" for absolute lowest BIN (like moulberry), "smooth" for IQR-filtered price. Default: raw.
	/// </summary>
	[QueryParam, DefaultValue("raw")]
	public string? Mode { get; set; }
}

internal sealed class GetAuctionHouseNeuEndpoint(
	DataContext context,
	IOptions<AuctionHouseSettings> auctionHouseSettings
) : Endpoint<GetAuctionHouseNeuRequest, Dictionary<string, long>>
{
	private readonly AuctionHouseSettings _config = auctionHouseSettings.Value;

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
		var useRaw = !string.Equals(req.Mode, "smooth", StringComparison.OrdinalIgnoreCase);
		
		var recentCutoff = DateTime.UtcNow.AddDays(-_config.AggregationMaxLookbackDays);
		var lastLowestCutoff = DateTime.UtcNow.AddYears(-1);

		var items = await context.AuctionItems.AsNoTracking()
			.Where(r => r.CalculatedAt >= recentCutoff
			            || (r.LastLowestAt != null && r.LastLowestAt >= lastLowestCutoff))
			.ToListAsync(c);

		var result = new Dictionary<string, long>();

		foreach (var item in items) {
			var neuName = NeuInternalNameConverter.ToNeuInternalName(item.SkyblockId, item.VariantKey);
			if (neuName is null) continue;

			var price = useRaw ? GetRawPrice(item) : GetSmoothedPrice(item);
			if (price is null or <= 0) continue;

			var longPrice = (long)price.Value;

			// If multiple variants map to same NEU name, keep lowest price
			if (!result.TryGetValue(neuName, out var existing) || longPrice < existing) {
				result[neuName] = longPrice;
			}
		}

		await Send.OkAsync(result, c);
	}

	private static decimal? GetRawPrice(AuctionItem item) {
		if (item.RawLowest is > 0) return item.RawLowest;
		// Fall back to smoothed values if no raw data available
		return GetSmoothedPrice(item);
	}

	private static decimal? GetSmoothedPrice(AuctionItem item) {
		if (item.Lowest is > 0) return item.Lowest;
		if (item.Lowest3Day is > 0) return item.Lowest3Day;
		if (item.Lowest7Day is > 0) return item.Lowest7Day;
		if (item.LastLowest is > 0) return item.LastLowest;
		return null;
	}
}
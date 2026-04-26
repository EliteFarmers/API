using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EliteAPI.Features.Resources.Auctions.Services;

[RegisterService<AuctionHouseNeuService>(LifeTime.Scoped)]
public sealed class AuctionHouseNeuService(
	DataContext context,
	IOptions<AuctionHouseSettings> auctionHouseSettings,
	ILogger<AuctionHouseNeuService> logger,
	TimeProvider timeProvider)
{
	private const int MinSamplesForFreshOnlyFallback = 5;

	internal sealed class FallbackPriceAccumulator
	{
		public List<decimal> FreshPrices { get; } = [];
		public List<decimal> StalePrices { get; } = [];
	}

	private readonly AuctionHouseSettings _config = auctionHouseSettings.Value;

	public async Task<Dictionary<string, long>> BuildPricesAsync(string? mode, CancellationToken cancellationToken = default) {
		var useRaw = !string.Equals(mode, "smooth", StringComparison.OrdinalIgnoreCase);
		var now = timeProvider.GetUtcNow().UtcDateTime;
		var recentCutoff = now.AddDays(-_config.AggregationMaxLookbackDays);
		var lastLowestCutoff = now.AddYears(-1);

		var items = await context.AuctionItems.AsNoTracking()
			.Where(r => r.CalculatedAt >= recentCutoff
			            || (r.LastLowestAt != null && r.LastLowestAt >= lastLowestCutoff))
			.ToListAsync(cancellationToken);

		var result = new Dictionary<string, long>();
		var fallbackPrices = new Dictionary<string, FallbackPriceAccumulator>();

		foreach (var item in items) {
			var price = useRaw ? GetRawPrice(item) : GetSmoothedPrice(item);
			if (price is null or <= 0) continue;

			var longPrice = (long)price.Value;
			var isFresh = item.CalculatedAt >= recentCutoff;
			AddNeuPrice(result, fallbackPrices, item.SkyblockId, item.VariantKey, longPrice, isFresh);
		}

		ApplyFallbackPrices(result, fallbackPrices, logger);
		return result;
	}

	public async Task<Dictionary<string, long>> BuildAverageLowestBinPricesAsync(int windowDays,
		CancellationToken cancellationToken = default) {
		var cutoff = timeProvider.GetUtcNow().AddDays(-windowDays).ToUnixTimeMilliseconds();

		// Raw SQL so that the AVG result is cast to numeric(38,4) before Npgsql reads it.
		// EF's AVG translation returns unbounded numeric whose precision can exceed C# decimal's
		// 28-29 significant digit limit, causing an OverflowException during materialisation.
		var averagedPrices = await context.Database
			.SqlQuery<AveragedLowestBinPriceRow>($"""
			    SELECT
			        "SkyblockId",
			        "VariantKey",
			        CAST(AVG("LowestBinPrice") AS numeric(38,4)) AS "AverageLowestBinPrice"
			    FROM "AuctionPriceHistories"
			    WHERE "BucketStart" >= {cutoff}
			      AND "LowestBinPrice" IS NOT NULL
			      AND "LowestBinPrice" > 0
			      AND "LowestBinPrice" = "LowestBinPrice"
			    GROUP BY "SkyblockId", "VariantKey"
			    """)
			.ToListAsync(cancellationToken);

		return BuildAverageLowestBinPrices(averagedPrices, logger);
	}

	internal static Dictionary<string, long> BuildAverageLowestBinPrices(
		IEnumerable<AveragedLowestBinPriceRow> averagedPrices,
		ILogger? logger = null) {

		var result = new Dictionary<string, long>();
		var fallbackPrices = new Dictionary<string, FallbackPriceAccumulator>();

		foreach (var item in averagedPrices) {
			if (item.AverageLowestBinPrice <= 0) continue;

			AddNeuPrice(result, fallbackPrices, item.SkyblockId, item.VariantKey, (long)item.AverageLowestBinPrice,
				isFresh: true);
		}

		ApplyFallbackPrices(result, fallbackPrices, logger);
		return result;
	}

	internal static int? TryParseAverageLowestBinWindow(string? window) {
		return window?.Trim().ToLowerInvariant() switch {
			"1" or "1d" or "1day" => 1,
			"3" or "3d" or "3day" => 3,
			"7" or "7d" or "7day" => 7,
			_ => null
		};
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

	internal static void AddNeuPrice(Dictionary<string, long> result,
		Dictionary<string, FallbackPriceAccumulator> fallbackPrices,
		string skyblockId, string variantKey, long price, bool isFresh) {
		var neuName = NeuInternalNameConverter.ToNeuInternalName(skyblockId, variantKey);
		if (neuName is null) return;

		UpsertLowerPrice(result, neuName, price);

		foreach (var fallbackNeuName in GetFallbackNeuInternalNames(neuName)) {
			UpsertFallbackPrice(fallbackPrices, fallbackNeuName, price, isFresh);
		}
	}

	internal static IEnumerable<string> GetFallbackNeuInternalNames(string neuName) {
		var plusIndex = neuName.IndexOf('+');
		if (plusIndex > 0) {
			yield return neuName[..plusIndex];
		}
	}

	internal static long? GetFallbackPrice(FallbackPriceAccumulator prices, string neuName,
		ILogger? logger = null) {
		List<decimal> selectedPrices;
		if (prices.FreshPrices.Count >= MinSamplesForFreshOnlyFallback) {
			selectedPrices = prices.FreshPrices;
		}
		else if (prices.FreshPrices.Count + prices.StalePrices.Count >= MinSamplesForFreshOnlyFallback) {
			selectedPrices = [..prices.FreshPrices, ..prices.StalePrices];
		}
		else {
			selectedPrices = prices.FreshPrices.Count > 0 ? prices.FreshPrices : prices.StalePrices;
		}

		if (selectedPrices.Count == 0) return null;

		var iqrInputPrices = selectedPrices
			.OrderBy(price => price)
			.Take(10)
			.ToList();

		var representativeLowest = PriceCalculationHelpers.GetRepresentativeLowestFromList(
			iqrInputPrices,
			logger ?? NullLogger.Instance,
			neuName,
			"fallback");

		return representativeLowest.LowestPrice is > 0 ? (long)representativeLowest.LowestPrice.Value : null;
	}

	internal static void ApplyFallbackPrices(Dictionary<string, long> result,
		Dictionary<string, FallbackPriceAccumulator> fallbackPrices, ILogger? logger = null) {
		foreach (var (neuName, fallbackPrice) in fallbackPrices) {
			if (result.ContainsKey(neuName)) {
				continue;
			}

			var representativePrice = GetFallbackPrice(fallbackPrice, neuName, logger);
			if (representativePrice is not null) {
				result[neuName] = representativePrice.Value;
			}
		}
	}

	private static void UpsertLowerPrice(Dictionary<string, long> prices, string neuName, long price) {
		if (!prices.TryGetValue(neuName, out var existing) || price < existing) {
			prices[neuName] = price;
		}
	}

	private static void UpsertFallbackPrice(Dictionary<string, FallbackPriceAccumulator> prices, string neuName, long price,
		bool isFresh) {
		if (!prices.TryGetValue(neuName, out var existing)) {
			existing = new FallbackPriceAccumulator();
			prices[neuName] = existing;
		}

		if (isFresh) {
			existing.FreshPrices.Add(price);
		}
		else {
			existing.StalePrices.Add(price);
		}
	}

	internal sealed class AveragedLowestBinPriceRow
	{
		public string SkyblockId { get; set; } = string.Empty;
		public string VariantKey { get; set; } = string.Empty;
		public decimal AverageLowestBinPrice { get; set; }
	}
}

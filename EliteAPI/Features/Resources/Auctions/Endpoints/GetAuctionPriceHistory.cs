using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Auctions.Endpoints;

public class GetAuctionPriceHistory(DataContext context)
	: Endpoint<GetAuctionPriceHistoryRequest, GetAuctionPriceHistoryResponse>
{
	public override void Configure() {
		Get("/resources/auctions/{skyblockId}/{variantKey}");
		AllowAnonymous();

		Summary(s => { s.Summary = "Get Auction History For Item"; });
	}

	public override async Task HandleAsync(GetAuctionPriceHistoryRequest r, CancellationToken c) {
		var timespan = r.Timespan ?? "7d";
		var timespanDays = timespan switch {
			"1d" => 1,
			"3d" => 3,
			"7d" => 7,
			"14d" => 14,
			"30d" => 30,
			_ => 7
		};

		var variantKey = NormalizeVariantKey(r.VariantKey);
		var cutoff = DateTimeOffset.UtcNow.AddDays(-timespanDays).ToUnixTimeMilliseconds();

		List<PriceHistoryDataPoint> history;

		if (TryParseBundleKey(variantKey, out var bundle)) {
			history = await FetchBundleHistory(r.SkyblockId, bundle, cutoff, c);
		}
		else {
			history = await context.AuctionPriceHistories
				.AsNoTracking()
				.Where(h => h.SkyblockId == r.SkyblockId && h.VariantKey == variantKey && h.BucketStart >= cutoff)
				.OrderBy(h => h.BucketStart)
				.Select(h => new PriceHistoryDataPoint {
					Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(h.BucketStart),
					LowestBinPrice = h.LowestBinPrice,
					AverageBinPrice = h.AverageBinPrice,
					BinListings = h.BinListings,
					LowestSalePrice = h.LowestSalePrice,
					AverageSalePrice = h.AverageSalePrice,
					SaleAuctions = h.SaleAuctions,
					ItemsSold = h.ItemsSold
				})
				.ToListAsync(c);
		}

		var response = new GetAuctionPriceHistoryResponse {
			History = history
		};

		await Send.OkAsync(response, c);
	}

	private async Task<List<PriceHistoryDataPoint>> FetchBundleHistory(string skyblockId, VariantBundleRequest bundle,
		long cutoff, CancellationToken ct) {
		var query = context.AuctionPriceHistories
			.AsNoTracking()
			.Where(h => h.SkyblockId == skyblockId && h.BucketStart >= cutoff);

		switch (bundle.Category) {
			case VariantBundleCategoryPet:
				var petPattern = $"%pet:{bundle.Identifier}%";
				query = query.Where(h => EF.Functions.ILike(h.VariantKey, petPattern));
				break;
			case VariantBundleCategoryRune:
				if (bundle.Level is { } level) {
					var runePattern = $"%ex:rune={bundle.Identifier}:{level}%";
					query = query.Where(h => EF.Functions.ILike(h.VariantKey, runePattern));
				}
				else {
					var runeTypePattern = $"%ex:rune={bundle.Identifier}:%";
					query = query.Where(h => EF.Functions.ILike(h.VariantKey, runeTypePattern));
				}

				break;
			default:
				return [];
		}

		var records = await query.ToListAsync(ct);
		if (records.Count == 0) return [];

		return records
			.GroupBy(h => h.BucketStart)
			.Select(group => new PriceHistoryDataPoint {
				Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(group.Key),
				LowestBinPrice = group.Min(x => x.LowestBinPrice),
				AverageBinPrice = WeightedAverage(group, x => x.AverageBinPrice, x => x.BinListings),
				BinListings = group.Sum(x => x.BinListings),
				LowestSalePrice = group.Min(x => x.LowestSalePrice),
				AverageSalePrice = WeightedAverage(group, x => x.AverageSalePrice, x => x.SaleAuctions),
				SaleAuctions = group.Sum(x => x.SaleAuctions),
				ItemsSold = group.Sum(x => x.ItemsSold)
			})
			.OrderBy(point => point.Timestamp)
			.ToList();
	}

	private static decimal? WeightedAverage(IEnumerable<AuctionPriceHistory> source,
		Func<AuctionPriceHistory, decimal?> selector,
		Func<AuctionPriceHistory, int> weightSelector) {
		decimal weightedTotal = 0;
		var weightSum = 0;

		foreach (var entry in source) {
			var value = selector(entry);
			var weight = weightSelector(entry);
			if (value is null || weight <= 0) continue;
			weightedTotal += value.Value * weight;
			weightSum += weight;
		}

		return weightSum > 0 ? weightedTotal / weightSum : null;
	}

	private static bool TryParseBundleKey(string variantKey, out VariantBundleRequest bundle) {
		bundle = default;
		if (string.IsNullOrWhiteSpace(variantKey)) return false;
		if (!variantKey.StartsWith("bundle:", StringComparison.OrdinalIgnoreCase)) return false;

		var parts = variantKey.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length < 3) return false;

		var category = parts[1].ToLowerInvariant();
		switch (category) {
			case VariantBundleCategoryPet:
				if (parts.Length < 3) return false;
				bundle = new VariantBundleRequest(VariantBundleCategoryPet, parts[2].ToUpperInvariant(), null);
				return true;
			case VariantBundleCategoryRune:
				var identifier = parts[2].ToUpperInvariant();
				int? level = null;
				if (parts.Length >= 4 && int.TryParse(parts[3], out var parsedLevel)) level = parsedLevel;
				bundle = new VariantBundleRequest(VariantBundleCategoryRune, identifier, level);
				return true;
			default:
				return false;
		}
	}

	private static string NormalizeVariantKey(string variantKey) {
		if (string.IsNullOrWhiteSpace(variantKey) || variantKey == "-" || variantKey == "default") return string.Empty;
		return Uri.UnescapeDataString(variantKey);
	}

	private const string VariantBundleCategoryPet = "pet";
	private const string VariantBundleCategoryRune = "rune";

	private readonly record struct VariantBundleRequest(string Category, string Identifier, int? Level);
}

public class GetAuctionPriceHistoryRequest
{
	public required string SkyblockId { get; set; }
	public required string VariantKey { get; set; }
	public string? Timespan { get; set; }
}

public class GetAuctionPriceHistoryResponse
{
	public required List<PriceHistoryDataPoint> History { get; set; }
}

public class PriceHistoryDataPoint
{
	public DateTimeOffset Timestamp { get; set; }
	public decimal? LowestBinPrice { get; set; }
	public decimal? AverageBinPrice { get; set; }
	public int BinListings { get; set; }
	public decimal? LowestSalePrice { get; set; }
	public decimal? AverageSalePrice { get; set; }
	public int SaleAuctions { get; set; }
	public int ItemsSold { get; set; }
}
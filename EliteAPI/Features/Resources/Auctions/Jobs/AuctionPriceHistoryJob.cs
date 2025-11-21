using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace EliteAPI.Features.Resources.Auctions.Jobs;

public class AuctionPriceHistoryJob(DataContext context, ILogger<AuctionPriceHistoryJob> logger) : ISelfConfiguringJob
{
	public static readonly JobKey Key = new(nameof(AuctionPriceHistoryJob));
	private const long HourBucketSizeMilliseconds = 60L * 60L * 1000L;
	private static readonly TimeSpan LookbackWindow = TimeSpan.FromDays(30);

	public static void Configure(IServiceCollectionQuartzConfigurator quartz, ConfigurationManager configuration)
	{
		quartz.AddJob<AuctionPriceHistoryJob>(builder => builder.WithIdentity(Key))
			.AddTrigger(trigger => {
				trigger.ForJob(Key);
				trigger.StartNow();
				trigger.WithSimpleSchedule(schedule => {
					schedule.WithIntervalInHours(1);
					schedule.RepeatForever();
				});
			});
	}

	public async Task Execute(IJobExecutionContext executionContext)
	{
		var cancellationToken = executionContext.CancellationToken;
		var now = DateTimeOffset.UtcNow;
		var cutoff = now.Subtract(LookbackWindow).ToUnixTimeMilliseconds();

		var aggregates = new Dictionary<(string SkyblockId, string VariantKey, long BucketStart), AggregateState>(StringTupleComparer.Instance);

		await AggregateBinListings(aggregates, cutoff, cancellationToken);
		await AggregateEndedAuctions(aggregates, cutoff, cancellationToken);

		if (aggregates.Count == 0) {
			logger.LogDebug("No auction data found for price history aggregation window.");
			return;
		}

		var skyblockIds = aggregates.Keys.Select(k => k.SkyblockId).Distinct().ToList();
		var bucketStarts = aggregates.Keys.Select(k => k.BucketStart).Distinct().ToList();

		var existingRecords = await context.AuctionPriceHistories
			.Where(h => skyblockIds.Contains(h.SkyblockId) && bucketStarts.Contains(h.BucketStart))
			.ToListAsync(cancellationToken);

		var existingLookup = existingRecords.ToDictionary(h => (h.SkyblockId, h.VariantKey, h.BucketStart),
			StringTupleComparer.Instance);
		var updated = 0;
		var created = 0;

		foreach (var (key, aggregate) in aggregates) {
			var binAverage = aggregate.BinCount > 0
				? aggregate.BinPriceTotal / aggregate.BinCount
				: (decimal?)null;
			var saleAverage = aggregate.SaleItemTotal > 0
				? aggregate.SalePriceTotal / aggregate.SaleItemTotal
				: (decimal?)null;

			if (existingLookup.TryGetValue(key, out var record)) {
				record.LowestBinPrice = aggregate.LowestBinPrice;
				record.AverageBinPrice = binAverage;
				record.BinListings = aggregate.BinCount;
				record.LowestSalePrice = aggregate.LowestSalePrice;
				record.AverageSalePrice = saleAverage;
				record.SaleAuctions = aggregate.SaleCount;
				record.ItemsSold = aggregate.SaleItemTotal;
				record.CalculatedAt = now;
				updated++;
			}
			else {
				var history = new AuctionPriceHistory {
					SkyblockId = key.SkyblockId,
					VariantKey = key.VariantKey,
					BucketStart = key.BucketStart,
					LowestBinPrice = aggregate.LowestBinPrice,
					AverageBinPrice = binAverage,
					BinListings = aggregate.BinCount,
					LowestSalePrice = aggregate.LowestSalePrice,
					AverageSalePrice = saleAverage,
					SaleAuctions = aggregate.SaleCount,
					ItemsSold = aggregate.SaleItemTotal,
					CalculatedAt = now
				};

				context.AuctionPriceHistories.Add(history);
				created++;
			}
		}

		if (created == 0 && updated == 0) {
			logger.LogDebug("Auction price history aggregation produced no data changes.");
			return;
		}

		await context.SaveChangesAsync(cancellationToken);
		logger.LogInformation("Auction price history aggregation wrote {Created} new and {Updated} existing buckets.", created,
			updated);
	}

	private async Task AggregateBinListings(Dictionary<(string SkyblockId, string VariantKey, long BucketStart), AggregateState> aggregates,
		long cutoff, CancellationToken cancellationToken) {
		var binListings = await context.AuctionBinPrices
			.AsNoTracking()
			.Where(p => p.ListedAt >= cutoff)
			.Select(p => new {
				SkyblockId = p.SkyblockId!,
				VariantKey = p.VariantKey ?? string.Empty,
				BucketStart = p.ListedAt - (p.ListedAt % HourBucketSizeMilliseconds),
				p.Price
			})
			.ToListAsync(cancellationToken);

		foreach (var listing in binListings) {
			var key = (listing.SkyblockId, listing.VariantKey, listing.BucketStart);
			var state = aggregates.TryGetValue(key, out var existing)
				? existing
				: aggregates[key] = new AggregateState();

			state.LowestBinPrice = state.LowestBinPrice.HasValue
				? Math.Min(state.LowestBinPrice.Value, listing.Price)
				: listing.Price;
			state.BinPriceTotal += listing.Price;
			state.BinCount += 1;
		}
	}

	private async Task AggregateEndedAuctions(Dictionary<(string SkyblockId, string VariantKey, long BucketStart), AggregateState> aggregates,
		long cutoff, CancellationToken cancellationToken) {
		var endedAuctions = await context.Auctions
			.AsNoTracking()
			.Where(a => a.Bin && a.SkyblockId != null && a.SoldAt >= cutoff && a.BuyerUuid != null)
			.Select(a => new {
				SkyblockId = a.SkyblockId!,
				VariantKey = a.VariantKey ?? string.Empty,
				BucketStart = a.SoldAt - (a.SoldAt % HourBucketSizeMilliseconds),
				PricePerUnit = a.Count > 0 ? (decimal)a.Price / a.Count : (decimal)a.Price,
				ItemsSold = a.Count > 0 ? (int)a.Count : 1
			})
			.ToListAsync(cancellationToken);

		foreach (var auction in endedAuctions) {
			var key = (auction.SkyblockId, auction.VariantKey, auction.BucketStart);
			var state = aggregates.TryGetValue(key, out var existing)
				? existing
				: aggregates[key] = new AggregateState();

			state.LowestSalePrice = state.LowestSalePrice.HasValue
				? Math.Min(state.LowestSalePrice.Value, auction.PricePerUnit)
				: auction.PricePerUnit;
			state.SalePriceTotal += auction.PricePerUnit * auction.ItemsSold;
			state.SaleItemTotal += auction.ItemsSold;
			state.SaleCount += 1;
		}
	}

	private sealed class AggregateState
	{
		public decimal? LowestBinPrice { get; set; }
		public decimal BinPriceTotal { get; set; }
		public int BinCount { get; set; }
		public decimal? LowestSalePrice { get; set; }
		public decimal SalePriceTotal { get; set; }
		public int SaleItemTotal { get; set; }
		public int SaleCount { get; set; }
	}

	private sealed class StringTupleComparer : IEqualityComparer<(string SkyblockId, string VariantKey, long BucketStart)>
	{
		public static readonly StringTupleComparer Instance = new();

		public bool Equals((string SkyblockId, string VariantKey, long BucketStart) x,
			(string SkyblockId, string VariantKey, long BucketStart) y) {
			return string.Equals(x.SkyblockId, y.SkyblockId, StringComparison.OrdinalIgnoreCase)
			       && string.Equals(x.VariantKey, y.VariantKey, StringComparison.Ordinal)
			       && x.BucketStart == y.BucketStart;
		}

		public int GetHashCode((string SkyblockId, string VariantKey, long BucketStart) obj) {
			return HashCode.Combine(obj.SkyblockId.ToUpperInvariant(), obj.VariantKey, obj.BucketStart);
		}
	}
}

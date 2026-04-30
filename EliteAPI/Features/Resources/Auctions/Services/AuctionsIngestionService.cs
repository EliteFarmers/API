using System.Text.Json;
using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Items.Models;
using EliteAPI.Parsers.Inventories;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Refit;
using StackExchange.Redis;
using ZLinq;

namespace EliteAPI.Features.Resources.Auctions.Services;

public record ItemVariantKey(string SkyblockId, string VariantKey);

[RegisterService<AuctionsIngestionService>(LifeTime.Scoped)]
public class AuctionsIngestionService(
	IHypixelApi hypixelApi,
	DataContext context,
	ILogger<AuctionsIngestionService> logger,
	IConnectionMultiplexer redis,
	VariantKeyGenerator variantKeyGenerator,
	IOptions<AuctionHouseSettings> auctionHouseSettings)
{
	private readonly AuctionHouseSettings _config = auctionHouseSettings.Value;
	private readonly Dictionary<int, long> _pagesLastUpdated = new();

	public async Task IngestAndStageAuctionDataAsync(int maxPages, CancellationToken cancellationToken = default) {
		logger.LogInformation("Starting auction data ingestion and staging...");
		var newRawPrices = new List<AuctionBinPrice>();
		var totalPages = 10;
		var ingestionTime = DateTimeOffset.UtcNow;
		var ingestionTimeMs = ingestionTime.ToUnixTimeMilliseconds();
		var updatedExistingCount = 0;
		var batchSize = _config.PageBatchSize > 0 ? _config.PageBatchSize : 10;

		for (var batchStart = 0; batchStart < totalPages; batchStart += batchSize) {
			if (cancellationToken.IsCancellationRequested) {
				logger.LogInformation("Ingestion staging cancelled");
				break;
			}

			var batchEnd = Math.Min(batchStart + batchSize, totalPages);
			var pageTasks = new List<Task<(int Page, ApiResponse<AuctionHouseResponse>? Response)>>();

			for (var page = batchStart; page < batchEnd; page++) {
				var p = page;
				pageTasks.Add(Task.Run(async () => {
					var response = await hypixelApi.FetchAuctionHouseAsync(p, cancellationToken);
					return (Page: p, Response: (ApiResponse<AuctionHouseResponse>?)response);
				}, cancellationToken));
			}

			var results = await Task.WhenAll(pageTasks);

			foreach (var (page, response) in results.OrderBy(r => r.Page)) {
				if (response?.Content == null || !response.IsSuccessful) {
					logger.LogError(response?.Error,
						"Failed to fetch auctions for page {Page} or API call was unsuccessful", page);
					if (page == 0 && response is { IsSuccessful: false }) return;
					continue;
				}

				var apiResponse = response.Content;

				var lastUpdated = apiResponse.LastUpdated;
				if (_pagesLastUpdated.TryGetValue(page, out var lastUpdate) && lastUpdate >= lastUpdated) {
					// Mark auctions on skipped pages as seen still 
					var skippedBinUuids = apiResponse.Auctions
						.Where(a => a.Bin)
						.Select(a => Guid.Parse(a.Uuid))
						.ToList();
					if (skippedBinUuids.Count > 0) {
						await context.AuctionBinPrices
							.Where(p => skippedBinUuids.Contains(p.AuctionUuid))
							.ExecuteUpdateAsync(s => s
								.SetProperty(p => p.LastSeenAt, ingestionTimeMs)
								.SetProperty(p => p.IngestedAt, ingestionTime), cancellationToken);
					}
					logger.LogDebug("Skipping page {Page} as it has not been updated since last ingestion", page);
					continue;
				}

				_pagesLastUpdated[page] = lastUpdated;

				// Update totalPages from first page response
				if (page == 0) {
					totalPages = Math.Min(apiResponse.TotalPages, maxPages);
				}

				var auctionItems = apiResponse.Auctions
					.Select(a => new { a.Uuid, Item = NbtParser.NbtToItem(a.ItemBytes) })
					.ToDictionary(a => a.Uuid, a => a.Item);

				var newAuctionIds = apiResponse.Auctions
					.Select(au => Guid.Parse(au.Uuid))
					.ToList();

				var existingAuctions = await context.Auctions
					.Where(a => newAuctionIds.Contains(a.AuctionId))
					.ToDictionaryAsync(a => a.AuctionId, cancellationToken);
				
				var binAuctions = apiResponse.Auctions.Where(a => a.Bin).ToList();
				var binAuctionGuids = binAuctions.Select(a => Guid.Parse(a.Uuid)).ToList();
				var existingBinPrices = binAuctionGuids.Count > 0
					? await context.AuctionBinPrices
						.Where(p => binAuctionGuids.Contains(p.AuctionUuid))
						.ToDictionaryAsync(p => p.AuctionUuid, cancellationToken)
					: new Dictionary<Guid, AuctionBinPrice>();

				foreach (var auction in binAuctions) {
					if (!auctionItems.TryGetValue(auction.Uuid, out var itemDto)) continue;
					if (itemDto?.SkyblockId is null) continue;

					var variedBy = variantKeyGenerator.Generate(itemDto, auction.Tier ?? "COMMON");
					if (variedBy is null) continue;

					var count = itemDto.Count > 0 ? itemDto.Count : 1;
					var price = (decimal)auction.StartingBid / count;
					var auctionGuid = Guid.Parse(auction.Uuid);

					if (existingBinPrices.TryGetValue(auctionGuid, out var existingPrice)) {
						existingPrice.Price = price;
						existingPrice.LastSeenAt = ingestionTimeMs;
						existingPrice.IngestedAt = ingestionTime;
						updatedExistingCount++;
					}
					else {
						newRawPrices.Add(new AuctionBinPrice {
							SkyblockId = itemDto.SkyblockId,
							VariantKey = variedBy.ToKey(),
							Price = price,
							ListedAt = auction.Start,
							LastSeenAt = ingestionTimeMs,
							AuctionUuid = auctionGuid,
							IngestedAt = ingestionTime
						});
					}
				}

				foreach (var auction in apiResponse.Auctions) {
					if (!auction.Bin) continue;
					if (!auctionItems.TryGetValue(auction.Uuid, out var itemDto)) continue;
					if (itemDto?.SkyblockId is null) continue;

					var variedBy = variantKeyGenerator.Generate(itemDto, auction.Tier ?? "COMMON");
					if (variedBy is null) continue;

					var count = itemDto.Count > 0 ? itemDto.Count : 1;
					var price = (decimal)auction.StartingBid / count;
					var auctionId = Guid.Parse(auction.Uuid);

					if (!existingAuctions.TryGetValue(auctionId, out var existingAuction)) {
						var newAuction = new Auction {
							AuctionId = auctionId,
							SellerUuid = Guid.Parse(auction.Auctioneer),
							SellerProfileUuid = Guid.Parse(auction.ProfileId),
							Start = auction.Start,
							End = auction.End,
							Price = (long)price,
							StartingBid = auction.StartingBid,
							HighestBid = auction.HighestBidAmount,
							Count = (short)count,
							Bin = auction.Bin,
							ItemUuid = itemDto.Uuid != null ? Guid.Parse(itemDto.Uuid) : null,
							SkyblockId = itemDto.SkyblockId,
							VariantKey = variedBy.ToKey(),
							Item = Convert.FromBase64String(auction.ItemBytes),
							LastUpdatedAt = ingestionTime
						};
						context.Auctions.Add(newAuction);
					}
					else {
						if (existingAuction.Price != (long)price ||
						    existingAuction.HighestBid != auction.HighestBidAmount) {
							existingAuction.Price = (long)price;
							existingAuction.HighestBid = auction.HighestBidAmount;
							existingAuction.LastUpdatedAt = ingestionTime;
						}
					}
				}

				logger.LogDebug("Processed page {PageNumber}/{TotalPagesValue}", page + 1, totalPages);
			}
		}

		if (newRawPrices.Count != 0) {
			await context.AuctionBinPrices.AddRangeAsync(newRawPrices, cancellationToken);
		}

		if (newRawPrices.Count != 0 || updatedExistingCount > 0 || context.ChangeTracker.HasChanges()) {
			await context.SaveChangesAsync(cancellationToken);
			logger.LogInformation(
				"Staged {NewCount} new and refreshed {UpdatedCount} existing BIN auction prices",
				newRawPrices.Count, updatedExistingCount);
		}
		else {
			logger.LogInformation("No BIN auction price changes were staged");
		}

		await AggregateAuctionDataAsync(ingestionTimeMs, cancellationToken);
	}

	public async Task AggregateAuctionDataAsync(long? latestIngestionTimeMs = null,
		CancellationToken cancellationToken = default) {
		logger.LogInformation("Starting auction data aggregation...");
		var now = DateTimeOffset.UtcNow;
		var db = redis.GetDatabase();

		var lookback7DayMs = now.AddDays(-_config.LongTermRepresentativeLowestDays).ToUnixTimeMilliseconds();
		var lookback3DayMs = now.AddDays(-_config.ShortTermRepresentativeLowestDays).ToUnixTimeMilliseconds();
		var recentWindowMs = now.AddHours(-_config.RecentWindowHours).ToUnixTimeMilliseconds();
		var fallbackLookbackHours = _config.RecentFallbackMaxLookbackHours > 0
			? _config.RecentFallbackMaxLookbackHours
			: _config.RecentFallbackMaxLookbackDays * 24d;
		var fallbackWindowMs = now.AddHours(-fallbackLookbackHours).ToUnixTimeMilliseconds();
		
		var todayUtc = now.UtcDateTime.Date;
		var freshCutoffDate = todayUtc.AddDays(-1); // Yesterday 00:00 UTC — still receiving data
		var lookback7DayDate = todayUtc.AddDays(-_config.LongTermRepresentativeLowestDays);
		
		var freshCutoffMs = new DateTimeOffset(freshCutoffDate, TimeSpan.Zero).ToUnixTimeMilliseconds();
		var freshPrices = await context.AuctionBinPrices
			.Where(r => r.LastSeenAt >= freshCutoffMs && r.Price > 0)
			.Select(r => new { r.SkyblockId, r.VariantKey, LastSeenAt = r.LastSeenAt, r.Price })
			.ToListAsync(cancellationToken);

		var cachedPrices = new List<CachedBinPrice>();
		for (var date = lookback7DayDate; date < freshCutoffDate; date = date.AddDays(1)) {
			var dayStart = new DateTimeOffset(date, TimeSpan.Zero).ToUnixTimeMilliseconds();
			var dayEnd = new DateTimeOffset(date.AddDays(1), TimeSpan.Zero).ToUnixTimeMilliseconds();

			if (dayEnd <= lookback7DayMs) continue;

			var cacheKey = $"auction-prices:daily:v2:{date:yyyy-MM-dd}";
			var cached = await db.StringGetAsync(cacheKey);

			if (cached.HasValue) {
				var dayPrices = JsonSerializer.Deserialize<List<CachedBinPrice>>(cached.ToString());
				if (dayPrices is not null) {
					cachedPrices.AddRange(dayPrices);
					continue;
				}
			}

			var dayDbPrices = await context.AuctionBinPrices
				.Where(r => r.LastSeenAt >= dayStart && r.LastSeenAt < dayEnd && r.Price > 0)
				.Select(r => new CachedBinPrice(r.SkyblockId, r.VariantKey, r.LastSeenAt, r.Price))
				.ToListAsync(cancellationToken);

			var serialized = JsonSerializer.Serialize(dayDbPrices);
			await db.StringSetAsync(cacheKey, serialized, TimeSpan.FromDays(8));

			cachedPrices.AddRange(dayDbPrices);
			logger.LogInformation("Cached {Count} bin prices for {Date}", dayDbPrices.Count,
				date.ToString("yyyy-MM-dd"));
		}

		// Combine fresh and cached prices then group by variant
		var allPrices = freshPrices
			.Select(p => new CachedBinPrice(p.SkyblockId, p.VariantKey, p.LastSeenAt, p.Price))
			.Concat(cachedPrices)
			.ToList();

		var grouped = allPrices
			.GroupBy(r => (r.SkyblockId, r.VariantKey))
			.ToList();

		logger.LogInformation(
			"Aggregating {PriceCount} bin prices for {VariantCount} variants ({FreshCount} fresh, {CachedCount} cached)",
			allPrices.Count, grouped.Count, freshPrices.Count, cachedPrices.Count);

		var itemKeysToProcess = grouped
			.AsValueEnumerable()
			.Select(g => g.Key.SkyblockId)
			.Distinct()
			.ToList();

		var existingItemsDict = await context.SkyblockItems
			.AsNoTracking()
			.Where(p => itemKeysToProcess.Contains(p.ItemId))
			.ToDictionaryAsync(p => p.ItemId, cancellationToken);

		var existingSummaries = await context.AuctionItems
			.Where(s => itemKeysToProcess.Contains(s.SkyblockId))
			.ToDictionaryAsync(s => (s.SkyblockId, s.VariantKey), cancellationToken);

		foreach (var group in grouped) {
			if (cancellationToken.IsCancellationRequested) {
				logger.LogInformation("Aggregation cancelled");
				break;
			}

			var (skyblockId, variantKey) = group.Key;
			var variantPrices = group.ToList();

			// Calculate all three price windows from cached data
			var prices7Day = variantPrices.Select(p => p.Price).ToList();
			var prices3Day = variantPrices.Where(p => p.LastSeenAt >= lookback3DayMs).Select(p => p.Price).ToList();
			var recentPrices = variantPrices.Where(p => p.LastSeenAt >= recentWindowMs).Select(p => p.Price).ToList();

			var (lowest7Day, volume7Day) = PriceCalculationHelpers.GetRepresentativeLowestFromList(
				prices7Day, logger, skyblockId, variantKey);
			var (lowest3Day, volume3Day) = PriceCalculationHelpers.GetRepresentativeLowestFromList(
				prices3Day, logger, skyblockId, variantKey);
			var (recentLowest, recentVolume) = PriceCalculationHelpers.GetRepresentativeLowestFromList(
				recentPrices, logger, skyblockId, variantKey);

			// Only use still active auctions for the raw lowest if we have any
			// Otherwise fallback to the recent window
			decimal? rawLowest = null;
			if (latestIngestionTimeMs is { } activeCutoffMs) {
				var activePrices = variantPrices
					.Where(p => p.LastSeenAt >= activeCutoffMs)
					.Select(p => p.Price)
					.ToList();
				if (activePrices.Count > 0) {
					rawLowest = activePrices.Min();
				}
			}
			
			if (rawLowest is null && recentPrices.Count > 0) {
				rawLowest = recentPrices.Min();
			}
			
			if (!recentLowest.HasValue || recentVolume < _config.MinRecentVolumeThreshold) {
				var fallbackPrices = variantPrices
					.Where(p => p.LastSeenAt >= fallbackWindowMs)
					.OrderByDescending(p => p.LastSeenAt)
					.Take(_config.RecentFallbackTakeCount)
					.Select(p => p.Price)
					.ToList();

				var (fallbackLowest, fallbackVolume) = PriceCalculationHelpers.GetRepresentativeLowestFromList(
					fallbackPrices, logger, skyblockId, variantKey);
				if (fallbackLowest.HasValue) {
					recentLowest = fallbackLowest;
					recentVolume = fallbackVolume;
				}
			}

			if (!existingItemsDict.ContainsKey(skyblockId)) {
				var newItem = new SkyblockItem(skyblockId);
				context.SkyblockItems.Add(newItem);
				existingItemsDict[skyblockId] = newItem;
				await context.SaveChangesAsync(cancellationToken);
			}

			if (!existingSummaries.TryGetValue((skyblockId, variantKey), out var summary)) {
				summary = new AuctionItem {
					SkyblockId = skyblockId,
					VariantKey = variantKey
				};
				context.AuctionItems.Add(summary);
				existingSummaries[(skyblockId, variantKey)] = summary;
			}

			// Persist the last known valid lowest price before overwriting
			if (recentLowest.HasValue) {
				summary.LastLowest = recentLowest;
				summary.LastLowestAt = now;
			}

			summary.Lowest = recentLowest;
			summary.LowestVolume = recentVolume;
			summary.LowestObservedAt = recentLowest.HasValue ? now : null;
			summary.RawLowest = rawLowest;
			summary.Lowest3Day = lowest3Day;
			summary.Lowest3DayVolume = volume3Day;
			summary.Lowest7Day = lowest7Day;
			summary.Lowest7DayVolume = volume7Day;
			summary.CalculatedAt = now;
		}

		await context.SaveChangesAsync(cancellationToken);
		logger.LogInformation("Auction data aggregation complete for {Count} processed variants", grouped.Count);
		await CleanUpOldRawAuctionDataAsync(TimeSpan.FromDays(_config.RawDataRetentionDays), cancellationToken);
	}
	
	private record CachedBinPrice(string SkyblockId, string VariantKey, long LastSeenAt, decimal Price);

	public async Task TriggerUpdate() {
		var db = redis.GetDatabase();
		const string cacheKey = "auction-ingestion:trigger";

		if (!await db.KeyExistsAsync(cacheKey)) {
			logger.LogInformation("Triggering auction data ingestion and aggregation");
			await db.StringSetAsync(cacheKey, "1", TimeSpan.FromSeconds(_config.AuctionsRefreshInterval));
			await IngestAndStageAuctionDataAsync(int.MaxValue);
		}
		else {
			logger.LogDebug("Auction ingestion already in progress, skipping trigger");
		}
	}

	private async Task CleanUpOldRawAuctionDataAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken) {
		var cutoffIngestedDate = DateTimeOffset.UtcNow.Subtract(retentionPeriod);
		var recordsToDelete = await context.AuctionBinPrices
			.Where(r => r.IngestedAt < cutoffIngestedDate)
			.ExecuteDeleteAsync(cancellationToken);

		logger.LogInformation("Cleaned up {Count} old raw auction data records", recordsToDelete);
	}
}
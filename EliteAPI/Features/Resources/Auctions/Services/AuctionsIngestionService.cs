using EliteAPI.Configuration.Settings;
using EliteAPI.Data;
using EliteAPI.Features.Resources.Auctions.Models;
using EliteAPI.Features.Resources.Items.Models;
using EliteAPI.Parsers.Inventories;
using FastEndpoints;
using HypixelAPI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ZLinq;

namespace EliteAPI.Features.Resources.Auctions.Services;

public record ItemVariantKey(string SkyblockId, string VariantKey); // Helper record

[RegisterService<AuctionsIngestionService>(LifeTime.Scoped)]
public class AuctionsIngestionService(
    IHypixelApi hypixelApi,
    DataContext context,
    ILogger<AuctionsIngestionService> logger,
    VariantKeyGenerator variantKeyGenerator,
    IOptions<AuctionHouseSettings> auctionHouseSettings)
{
    private readonly AuctionHouseSettings _config = auctionHouseSettings.Value;
    private readonly Dictionary<int, long> _pagesLastUpdated = new();
    
    public async Task IngestAndStageAuctionDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting auction data ingestion and staging...");
        var allSeenAuctionUuidsThisRun = new HashSet<string>();
        var newRawPrices = new List<AuctionBinPrice>();
        var totalPages = 1;
        var ingestionTime = DateTimeOffset.UtcNow;

        for (var page = 0; page < totalPages; page++)
        {
            if (cancellationToken.IsCancellationRequested) { logger.LogInformation("Ingestion staging cancelled"); break; }

            var response = await hypixelApi.FetchAuctionHouse(page);
            if (response.Content == null || !response.IsSuccessful)
            {
                logger.LogError("Failed to fetch auctions for page {Page} or API call was unsuccessful", page);
                if (page == 0 && response is { IsSuccessful: false }) break;
                continue;
            }
            var apiResponse = response.Content;
            
            var lastUpdated = apiResponse.LastUpdated;
            if (_pagesLastUpdated.TryGetValue(page, out var lastUpdate) && lastUpdate >= lastUpdated)
            {
                logger.LogDebug("Skipping page {Page} as it has not been updated since last ingestion", page);
                continue;
            }
            _pagesLastUpdated[page] = lastUpdated;
            
            totalPages = apiResponse.TotalPages;
            
            var auctionItems = await apiResponse.Auctions.ToAsyncEnumerable()
                .SelectAwait(async a => new { a.Uuid, Item = await NbtParser.NbtToItem(a.ItemBytes) })
                .ToDictionaryAsync(a => a.Uuid, a => a.Item, cancellationToken: cancellationToken);

            foreach (var auction in apiResponse.Auctions)
            {
                if (!auction.Bin || !allSeenAuctionUuidsThisRun.Add(auction.Uuid) || !auctionItems.TryGetValue(auction.Uuid, out var itemDto)) continue;
                if (itemDto?.SkyblockId is null) continue;

                var variantKey = variantKeyGenerator.Generate(itemDto, auction.Tier ?? "COMMON");
                if (variantKey is null) continue; // No SkyblockId found
                decimal price = auction.StartingBid;

                newRawPrices.Add(new AuctionBinPrice
                {
                    SkyblockId = itemDto.SkyblockId,
                    VariantKey = variantKey,
                    Price = price,
                    ListedAtUnixMillis = auction.Start,
                    AuctionUuid = Guid.Parse(auction.Uuid),
                    IngestedAtUtc = ingestionTime
                });
            }
            
            logger.LogDebug("Processed page {PageNumber}/{TotalPagesValue}", page + 1, totalPages);
            
            // Wait 500ms between pages to be nice to the API
            await Task.Delay(500, cancellationToken);
        }

        if (newRawPrices.Count != 0)
        {
            await context.AuctionBinPrices.AddRangeAsync(newRawPrices, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Staged {Count} new raw BIN auction prices", newRawPrices.Count);
        } else {
            logger.LogInformation("No new raw BIN auction prices were staged");
        }
        
        await AggregateAuctionDataAsync(cancellationToken);
    }

    public async Task AggregateAuctionDataAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting auction data aggregation...");
        var now = DateTimeOffset.UtcNow;

        var itemsToProcess = await GetVariantsToUpdateAsync(now, _config.AggregationMaxLookbackDays, cancellationToken);
        logger.LogInformation("Found {Count} unique item/variant combinations to aggregate", itemsToProcess.Count);

        var itemKeysToProcess = itemsToProcess
            .AsValueEnumerable()
            .Select(v => v.SkyblockId)
            .Distinct()
            .ToList();
        
        // Fetch existing items for the products we are about to process
        // Needed to ensure related item exists in the database
        var existingItemsDict = await context.SkyblockItems
            .AsNoTracking()
            .Where(p => itemKeysToProcess.Contains(p.ItemId))
            .ToDictionaryAsync(p => p.ItemId, cancellationToken);

        foreach (var itemKey in itemsToProcess)
        {
            if (cancellationToken.IsCancellationRequested) { logger.LogInformation("Aggregation cancelled"); break; }

            var (recentLowest, recentVolume) = await CalculateRecentRepresentativeLowestPriceAsync(
                itemKey.SkyblockId, itemKey.VariantKey, now, cancellationToken);

            var (lowest3Day, volume3Day) = await CalculateRepresentativeLowestForPeriodAsync(
                itemKey.SkyblockId, itemKey.VariantKey, now.AddDays(-_config.ShortTermRepresentativeLowestDays).ToUnixTimeMilliseconds(), cancellationToken);

            var (lowest7Day, volume7Day) = await CalculateRepresentativeLowestForPeriodAsync(
                itemKey.SkyblockId, itemKey.VariantKey, now.AddDays(-_config.LongTermRepresentativeLowestDays).ToUnixTimeMilliseconds(), cancellationToken);

            var summary = await context.AuctionItemVariantSummaries
                .FirstOrDefaultAsync(s => s.SkyblockId == itemKey.SkyblockId && s.VariantKey == itemKey.VariantKey, cancellationToken);

            if (!existingItemsDict.ContainsKey(itemKey.SkyblockId))
            {
                context.SkyblockItems.Add(new SkyblockItem(itemKey.SkyblockId));
                // Save changes here to ensure the item exists
                // This will also be very infrequent, so it's okay to do it here
                await context.SaveChangesAsync(cancellationToken); 
            }
            
            if (summary is null)
            {
                summary = new AuctionItemVariantSummary { SkyblockId = itemKey.SkyblockId, VariantKey = itemKey.VariantKey };
                context.AuctionItemVariantSummaries.Add(summary);
            }

            summary.RecentLowestPrice = recentLowest;
            summary.RecentLowestPriceVolume = recentVolume;
            summary.RecentLowestPriceObservationTime = recentLowest.HasValue ? now : null;
            summary.RepresentativeLowestPrice3Day = lowest3Day;
            summary.RepresentativeLowestPrice3DayVolume = volume3Day;
            summary.RepresentativeLowestPrice7Day = lowest7Day;
            summary.RepresentativeLowestPrice7DayVolume = volume7Day;
            summary.LastCalculatedUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Auction data aggregation complete for {Count} processed variants", itemsToProcess.Count);
        await CleanUpOldRawAuctionDataAsync(TimeSpan.FromDays(_config.RawDataRetentionDays), cancellationToken);
    }

    private async Task<(decimal? Lowest, int Volume)> CalculateRepresentativeLowestForPeriodAsync(
        string skyblockId, string variantKey, long periodStartUnixMillis, CancellationToken cancellationToken)
    {
        var pricesInPeriod = await context.AuctionBinPrices
            .Where(r => r.SkyblockId == skyblockId && r.VariantKey == variantKey &&
                        r.ListedAtUnixMillis >= periodStartUnixMillis && r.Price > 0)
            .Select(r => r.Price)
            .ToListAsync(cancellationToken);
        return PriceCalculationHelpers.GetRepresentativeLowestFromList(pricesInPeriod, logger, skyblockId, variantKey);
    }

    private async Task<(decimal? Lowest, int Volume)> CalculateRecentRepresentativeLowestPriceAsync(
        string skyblockId, string variantKey, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var primaryWindowStartMs = now.AddHours(-_config.RecentWindowHours).ToUnixTimeMilliseconds();
        var primaryWindowPrices = await context.AuctionBinPrices
            .Where(r => r.SkyblockId == skyblockId && r.VariantKey == variantKey &&
                        r.ListedAtUnixMillis >= primaryWindowStartMs && r.Price > 0)
            .Select(r => r.Price)
            .ToListAsync(cancellationToken);

        var (lowest, volume) = PriceCalculationHelpers.GetRepresentativeLowestFromList(primaryWindowPrices, logger, skyblockId, variantKey);

        if (lowest.HasValue && volume >= _config.MinRecentVolumeThreshold) return (lowest, volume);
        
        logger.LogDebug("Primary window for recent lowest price for {SkyblockId}/{VariantKey} insufficient (Volume: {Volume}). Attempting fallback", skyblockId, variantKey, volume);
        var fallbackMaxLookbackMs = now.AddDays(-_config.RecentFallbackMaxLookbackDays).ToUnixTimeMilliseconds();
        var fallbackPrices = await context.AuctionBinPrices
            .Where(r => r.SkyblockId == skyblockId && r.VariantKey == variantKey &&
                        r.ListedAtUnixMillis >= fallbackMaxLookbackMs && r.Price > 0)
            .OrderByDescending(r => r.ListedAtUnixMillis)
            .Select(r => r.Price)
            .Take(_config.RecentFallbackTakeCount)
            .ToListAsync(cancellationToken);
        
        var (fallbackLowest, fallbackVolume) = PriceCalculationHelpers.GetRepresentativeLowestFromList(fallbackPrices, logger, skyblockId, variantKey);
        return fallbackLowest.HasValue 
            ? (fallbackLowest, fallbackVolume) 
            : (lowest, volume);
    }
    
    private async Task<List<ItemVariantKey>> GetVariantsToUpdateAsync(DateTimeOffset now, int maxLookbackDays, CancellationToken cancellationToken)
    {
        var lookbackTimestampMs = now.AddDays(-maxLookbackDays).ToUnixTimeMilliseconds();
        // This simple version gets all variants with any raw data in the lookback.
        // More advanced: compare AuctionBinPrices.IngestedAtUtc with AuctionItemVariantSummary.LastCalculatedUtc
        return await context.AuctionBinPrices
            .Where(r => r.ListedAtUnixMillis >= lookbackTimestampMs) // or r.IngestedAtUtc > some_point
            .Select(r => new ItemVariantKey(r.SkyblockId, r.VariantKey))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task CleanUpOldRawAuctionDataAsync(TimeSpan retentionPeriod, CancellationToken cancellationToken)
    {
        var cutoffIngestedDate = DateTimeOffset.UtcNow.Subtract(retentionPeriod);
        var recordsToDelete = await context.AuctionBinPrices
            .Where(r => r.IngestedAtUtc < cutoffIngestedDate)
            .ExecuteDeleteAsync(cancellationToken); 
        
        logger.LogInformation("Cleaned up {Count} old raw auction data records", recordsToDelete);
    }
}
using EliteAPI.Data;
using EliteAPI.Features.Resources.Items.Models;
using EliteFarmers.HypixelAPI;
using EliteFarmers.HypixelAPI.DTOs;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Resources.Bazaar;

[RegisterService<BazaarIngestionService>(LifeTime.Scoped)]
public class BazaarIngestionService(
	IHypixelApi hypixelApi,
	DataContext context,
	ILogger<BazaarIngestionService> logger)
{
	private const int VwapSkipOrders = 2; // Orders to skip from the "top" of the relevant summary
	private const int VwapTakeOrders = 8; // Number of subsequent orders to use for VWAP

	public async Task IngestBazaarDataAsync() {
		var apiResponse = await hypixelApi.FetchBazaarAsync();
		if (!apiResponse.IsSuccessStatusCode || apiResponse.Content is not { Success: true }) {
			var errorContent = apiResponse.Error != null
				? apiResponse.Error.ToString()
				: "Unknown error";
			logger.LogError("Failed to fetch Bazaar data. Status: {StatusCode}. Error: {Error}",
				apiResponse.StatusCode, errorContent);
			return;
		}

		var bazaarData = apiResponse.Content;
		var recordedAt = DateTimeOffset.UtcNow;
		var newSnapshots = new List<BazaarProductSnapshot>();

		foreach (var (productId, productDetails) in bazaarData.Products) {
			var instaSellPrice = productDetails.QuickStatus.SellPrice;
			var instaBuyPrice = productDetails.QuickStatus.BuyPrice;

			var bids = productDetails.SellSummary
				.OrderByDescending(o => o.PricePerUnit)
				.ToList();
			var representativeBuyOrderPrice = GetRepresentativeOrderPrices(bids, instaSellPrice);

			var asks = productDetails.BuySummary
				.OrderBy(o => o.PricePerUnit)
				// Skip last order in list as it tends to be fake buy orders / storing coins
				.SkipLast(productDetails.BuySummary.Count > 2 ? 1 : 0)
				.ToList();
			var representativeSellOrderPrice = GetRepresentativeOrderPrices(asks, instaBuyPrice);

			newSnapshots.Add(new BazaarProductSnapshot {
				ProductId = productId,
				RecordedAt = recordedAt,
				InstaSellPrice = instaSellPrice,
				InstaBuyPrice = instaBuyPrice,
				BuyOrderPrice = representativeBuyOrderPrice,
				SellOrderPrice = representativeSellOrderPrice
			});
		}

		if (newSnapshots.Count != 0) {
			await context.BazaarProductSnapshots.AddRangeAsync(newSnapshots);
			await context.SaveChangesAsync();
			logger.LogInformation(
				"Successfully ingested {Count} Bazaar price snapshots",
				newSnapshots.Count);

			await CalculateAndStoreAveragesAsync(newSnapshots.Select(s => s.ProductId).Distinct().ToList(), recordedAt,
				TimeSpan.FromMinutes(20));
		}
	}

	private static double GetRepresentativeOrderPrices(List<BazaarOrder> bids, double instaSellPrice) {
		var volume = bids.Select(b => b.Amount).Sum();

		// Take the first price that is above 0.5% of the volume
		foreach (var bid in bids) {
			var percent = (double)bid.Amount / volume;
			if (percent > 0.005) return bid.PricePerUnit;
		}

		// Preform Vwap calculation if for some reason the above condition doesn't work.
		// The below can probably be removed at some point

		var bidSliceForVwap = bids.Skip(VwapSkipOrders).Take(VwapTakeOrders).ToList();
		var orderPrices = CalculateVwap(bidSliceForVwap);
		if (orderPrices != 0) return orderPrices;

		if (bids.Count != 0)
			orderPrices = CalculateVwap(bids.Take(VwapSkipOrders + VwapTakeOrders).ToList());
		if (orderPrices == 0) orderPrices = instaSellPrice;

		return orderPrices;
	}

	private static double CalculateVwap(List<BazaarOrder> orders) {
		if (orders.Count == 0) return 0;
		double totalValue = 0;
		long totalAmount = 0;

		foreach (var order in orders.Where(order => order.Amount > 0 && !(order.PricePerUnit <= 0))) {
			totalValue += order.PricePerUnit * order.Amount;
			totalAmount += order.Amount;
		}

		return totalAmount > 0 ? totalValue / totalAmount : 0;
	}

	public async Task CalculateAndStoreAveragesAsync(List<string> productIdsToUpdate, DateTimeOffset currentTime,
		TimeSpan lookbackDuration = default) {
		if (productIdsToUpdate.Count == 0) {
			logger.LogInformation("No products specified for average calculation");
			return;
		}

		if (lookbackDuration == TimeSpan.Zero) lookbackDuration = TimeSpan.FromHours(1);
		logger.LogInformation(
			"Starting calculation/update of averages for {ProductCount} products with lookback {LookbackDuration}",
			productIdsToUpdate.Count, lookbackDuration);

		// Fetch existing averages for the products we are about to process
		var existingAveragesDict = await context.BazaarProductSummaries
			.Where(p => productIdsToUpdate.Contains(p.ItemId))
			.ToDictionaryAsync(p => p.ItemId);

		var newCount = 0;
		var updatedCount = 0;

		foreach (var productId in productIdsToUpdate) {
			var relevantSnapshots = await context.BazaarProductSnapshots
				.Where(s => s.ProductId == productId && s.RecordedAt >= currentTime - lookbackDuration &&
				            s.RecordedAt <= currentTime)
				.OrderBy(s => s.RecordedAt)
				.ToListAsync();

			if (relevantSnapshots.Count == 0) {
				logger.LogDebug(
					"No relevant snapshots for product {ProductId} in window {WindowStart} to {WindowEnd}",
					productId, currentTime - lookbackDuration, currentTime);
				continue; // Skip if no data to calculate average from
			}

			var mostRecentSnapshot = relevantSnapshots.LastOrDefault();

			var avgInstaSell = CalculateResistantAverage(relevantSnapshots.Select(s => s.InstaSellPrice));
			var avgInstaBuy = CalculateResistantAverage(relevantSnapshots.Select(s => s.InstaBuyPrice));
			var avgRepBuy = CalculateResistantAverage(relevantSnapshots.Select(s => s.BuyOrderPrice));
			var avgRepSell =
				CalculateResistantAverage(relevantSnapshots.Select(s => s.SellOrderPrice));

			if (existingAveragesDict.TryGetValue(productId, out var existingAverage)) {
				// Update existing record
				if (mostRecentSnapshot is not null) {
					existingAverage.InstaSellPrice = mostRecentSnapshot.InstaSellPrice;
					existingAverage.InstaBuyPrice = mostRecentSnapshot.InstaBuyPrice;
					existingAverage.BuyOrderPrice = mostRecentSnapshot.BuyOrderPrice;
					existingAverage.SellOrderPrice = mostRecentSnapshot.SellOrderPrice;
				}

				existingAverage.AvgInstaSellPrice = avgInstaSell;
				existingAverage.AvgInstaBuyPrice = avgInstaBuy;
				existingAverage.AvgBuyOrderPrice = avgRepBuy;
				existingAverage.AvgSellOrderPrice = avgRepSell;
				existingAverage.CalculationTimestamp = currentTime;

				updatedCount++;
			}
			else {
				var item = await context.SkyblockItems.FirstOrDefaultAsync(i => i.ItemId == productId);
				item ??= new SkyblockItem(productId);

				// Insert new record
				var newAverage = new BazaarProductSummary {
					ItemId = productId,
					SkyblockItem = item,
					CalculationTimestamp = currentTime,
					AvgInstaSellPrice = avgInstaSell,
					AvgInstaBuyPrice = avgInstaBuy,
					AvgBuyOrderPrice = avgRepBuy,
					AvgSellOrderPrice = avgRepSell
				};

				if (mostRecentSnapshot is not null) {
					newAverage.InstaSellPrice = mostRecentSnapshot.InstaSellPrice;
					newAverage.InstaBuyPrice = mostRecentSnapshot.InstaBuyPrice;
					newAverage.BuyOrderPrice = mostRecentSnapshot.BuyOrderPrice;
					newAverage.SellOrderPrice = mostRecentSnapshot.SellOrderPrice;
				}

				context.BazaarProductSummaries.Add(newAverage);
				newCount++;
			}
		}

		if (newCount > 0 || updatedCount > 0) {
			await context.SaveChangesAsync();
			logger.LogInformation("Averages processed: {NewCount} new, {UpdatedCount} updated", newCount, updatedCount);
		}
		else {
			logger.LogInformation("No new averages to calculate or existing ones to update for the given products");
		}
	}

	private static double CalculateResistantAverage(IEnumerable<double> prices) {
		var priceList = prices.Where(p => p > 0).ToList();
		if (priceList.Count == 0) return 0;

		var sortedPrices = priceList.OrderBy(p => p).ToList();
		if (sortedPrices.Count < 3) return sortedPrices.Average();

		var itemsToTrim = (int)Math.Max(1, Math.Floor(sortedPrices.Count * 0.10));
		if (sortedPrices.Count <= itemsToTrim * 2) return sortedPrices.Average();

		var trimmedList = sortedPrices.Skip(itemsToTrim).Take(sortedPrices.Count - 2 * itemsToTrim).ToList();
		return trimmedList.Count != 0 ? trimmedList.Average() : sortedPrices.Average();
	}
}
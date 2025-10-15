using ZLinq;

namespace EliteAPI.Features.Resources.Auctions.Services;

public static class PriceCalculationHelpers
{
	// Minimum samples to attempt robust IQR. If fewer, a simpler min is used.
	private const int MinSamplesForIqr = 5;

	public static (decimal? LowestPrice, int Volume) GetRepresentativeLowestFromList(
		List<decimal>? prices,
		ILogger logger,
		string skyblockIdForLogging = "N/A",
		string variantKeyForLogging = "N/A") {
		if (prices is null || prices.Count == 0) return (null, 0);

		var sortedPrices = prices.AsValueEnumerable().OrderBy(p => p).ToList();
		var count = sortedPrices.Count;

		// Too few data points for a good IQR
		if (count < MinSamplesForIqr) return (sortedPrices.Min(), count);

		// Calculate Quartiles & IQR
		// Ensure indices are within bounds
		var q1 = sortedPrices[Math.Min(count - 1, (int)Math.Floor(count * 0.25))];
		var q3 = sortedPrices[Math.Min(count - 1, (int)Math.Floor(count * 0.75))];
		var iqr = q3 - q1;

		decimal lowerBound;
		decimal upperBound;

		// Standard IQR outlier rule: Q1 - 1.5 * IQR, Q3 + 1.5 * IQR
		if (iqr == 0) // Handles cases where many prices are identical (e.g. Q1=Median=Q3)
		{
			// If IQR is 0, the "cluster" is effectively items priced at Q1.
			// We want to include Q1. A very small tolerance can be used, or just equality.
			// To be safe and include items AT Q1/Q3 when IQR is 0:
			lowerBound = q1;
			upperBound = q3;
			// If there's truly no spread in the central 50% (Q1=Q3), then items outside this single price point
			// would need a non-zero IQR from other data to be included/excluded by the 1.5*IQR rule.
			// For "lowest representative", if many items are at price X (Q1), X is representative.
			logger.LogDebug("IQR is zero for {SkyblockId}/{VariantKey} (Q1={Q1}). Using Q1 as bounds for cluster",
				skyblockIdForLogging, variantKeyForLogging, q1);
		}
		else {
			lowerBound = q1 - 1.5m * iqr;
			upperBound = q3 + 1.5m * iqr;
		}

		// Ensure lower bound is not negative after calculation, stick to positive prices.
		lowerBound = Math.Max(0.01m, lowerBound); // Smallest possible positive price, effectively >0

		// Filter prices within the calculated "stable" range
		var clusterPrices = sortedPrices.Where(p => p >= lowerBound && p <= upperBound).ToList();

		if (clusterPrices.Count != 0) return (clusterPrices.Min(), clusterPrices.Count);

		logger.LogWarning(
			"IQR outlier removal resulted in an empty cluster for {SkyblockId}/{VariantKey}. Original count: {OriginalCount}. Q1={Q1}, Q3={Q3}, IQR={Iqr}. Bounds=[{LBound},{UBound}]. Falling back to median of valid positive prices",
			skyblockIdForLogging, variantKeyForLogging, prices.Count, q1, q3, iqr, lowerBound, upperBound);

		// Fallback to median of the valid positive prices if IQR yields no cluster
		if (sortedPrices.Count == 0) return (null, 0); // sortedPrices are the valid positive prices

		var median = sortedPrices[count / 2];
		var medianVolume =
			sortedPrices.Count(p => Math.Abs(p - median) < median * 0.05m + 0.01m); // Count items very close to median
		return (median, medianVolume > 0 ? medianVolume : sortedPrices.Count != 0 ? 1 : 0);
	}
}
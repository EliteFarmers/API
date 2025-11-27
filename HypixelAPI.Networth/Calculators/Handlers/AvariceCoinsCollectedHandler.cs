using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class AvariceCoinsCollectedHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		if (item.Attributes?.Extra == null) return false;

		if (item.Attributes.Extra.TryGetValue("collected_coins", out var collectedCoinsObj)) {
			return Convert.ToDouble(collectedCoinsObj) > 0;
		}

		return false;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		var zeroPrice = prices.GetValueOrDefault("CROWN_OF_AVARICE", 0);
		var billionPrice = prices.GetValueOrDefault("CROWN_OF_AVARICE_1B", 0);

		var collectedCoins = 0.0;
		if (item.Attributes?.Extra != null &&
		    item.Attributes.Extra.TryGetValue("collected_coins", out var collectedCoinsObj)) {
			collectedCoins = Convert.ToDouble(collectedCoinsObj);
		}

		var cappedCoins = Math.Min(collectedCoins, 1_000_000_000);
		var newPrice = zeroPrice + (billionPrice - zeroPrice) * (cappedCoins / 1_000_000_000);

		item.Calculation ??= new List<NetworthCalculation>();
		item.Calculation.Add(new NetworthCalculation {
			Id = "CROWN_OF_AVARICE",
			Type = "CROWN_OF_AVARICE",
			Value = newPrice,
			Count = (int)cappedCoins
		});

		item.BasePrice = newPrice;

		return 0;
	}
}
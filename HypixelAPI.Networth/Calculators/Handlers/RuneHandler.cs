using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class RuneHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Runes != null &&
		       item.Attributes.Runes.Count > 0 &&
		       item.SkyblockId != null &&
		       !item.SkyblockId.StartsWith("RUNE");
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Runes == null || item.Attributes.Runes.Count == 0) return new NetworthCalculationData();

		var (runeType, runeTier) = item.Attributes.Runes.First();

		var priceKey = $"RUNE_{runeType}_{runeTier}".ToUpper();

		if (prices.TryGetValue(priceKey, out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.Runes;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = priceKey,
				Type = "RUNE",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value, IsCosmetic = true };
		}

		return new NetworthCalculationData();
	}
}
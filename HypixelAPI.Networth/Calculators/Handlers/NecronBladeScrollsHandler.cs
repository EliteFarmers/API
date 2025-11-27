using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class NecronBladeScrollsHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.AbilityScrolls != null && item.Attributes.AbilityScrolls.Count > 0;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.AbilityScrolls == null) return 0;

		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		foreach (var scroll in item.Attributes.AbilityScrolls) {
			if (string.IsNullOrEmpty(scroll)) continue;

			if (prices.TryGetValue(scroll.ToUpper(), out var price)) {
				var value = price * NetworthConstants.ApplicationWorth.NecronBladeScroll;
				totalValue += value;

				item.Calculation.Add(new NetworthCalculation {
					Id = scroll,
					Type = "NECRON_SCROLL",
					Value = value,
					Count = 1
				});
			}
		}

		return totalValue;
	}
}
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class GemstonePowerScrollHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.ContainsKey("power_ability_scroll");
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (prices.TryGetValue("GEMSTONE_POWER_SCROLL", out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.GemstonePowerScroll;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "GEMSTONE_POWER_SCROLL",
				Type = "GEMSTONE_POWER_SCROLL",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
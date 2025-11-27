using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class GemstonePowerScrollHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("power_ability_scroll", out var scroll) &&
		       !string.IsNullOrEmpty(scroll.ToString());
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("power_ability_scroll", out var scrollObj)) {
			return 0;
		}

		var scroll = scrollObj.ToString();
		if (string.IsNullOrEmpty(scroll)) return 0;

		if (prices.TryGetValue(scroll, out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.GemstonePowerScroll;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = scroll,
				Type = "GEMSTONE_POWER_SCROLL",
				Value = value,
				Count = 1
			});

			return value;
		}

		return 0;
	}
}
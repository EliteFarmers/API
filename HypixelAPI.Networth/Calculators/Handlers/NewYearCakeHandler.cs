using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class NewYearCakeHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId == "NEW_YEAR_CAKE" &&
		       item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("new_year_cake_year", out var year);
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("new_year_cake_year", out var yearObj)) {
			return new NetworthCalculationData();
		}

		var year = AttributeHelper.ToInt32(yearObj);

		var cakeId = $"NEW_YEAR_CAKE_{year}";
		if (prices.TryGetValue(cakeId, out var price)) {
			var diff = price - item.BasePrice;
			item.BasePrice = price;

			return new NetworthCalculationData { Value = diff };
		}

		return new NetworthCalculationData();
	}
}
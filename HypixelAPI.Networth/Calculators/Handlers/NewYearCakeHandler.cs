using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class NewYearCakeHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId == "NEW_YEAR_CAKE";
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra.TryGetValue("new_years_cake", out var yearStr) is not true ||
		    !int.TryParse(yearStr.ToString(), out var year)) {
			return 0;
		}

		var cakeId = $"NEW_YEAR_CAKE_{year}";
		if (prices.TryGetValue(cakeId, out var price)) {
			var diff = price - item.Price;
			item.Price = price;
			item.BasePrice = price;

			return diff;
		}

		return 0;
	}
}
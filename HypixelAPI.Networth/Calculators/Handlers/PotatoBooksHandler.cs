using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PotatoBooksHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("hot_potato_count", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("hot_potato_count", out var countObj)) {
			return new NetworthCalculationData();
		}

		var potatoBookCount = AttributeHelper.ToInt32(countObj);
		var hotPotatoBookCount = Math.Min(potatoBookCount, 10);
		var totalValue = 0.0;

		item.Calculation ??= new List<NetworthCalculation>();

		if (prices.TryGetValue("HOT_POTATO_BOOK", out var hpbPrice)) {
			var value = hpbPrice * hotPotatoBookCount * NetworthConstants.ApplicationWorth.HotPotatoBook;
			totalValue += value;

			item.Calculation.Add(new NetworthCalculation {
				Id = "HOT_POTATO_BOOK",
				Type = "HOT_POTATO_BOOK",
				Value = value,
				Count = hotPotatoBookCount
			});
		}

		if (potatoBookCount > 10) {
			var fumingPotatoBookCount = potatoBookCount - 10;
			if (prices.TryGetValue("FUMING_POTATO_BOOK", out var fpbPrice)) {
				var value = fpbPrice * fumingPotatoBookCount * NetworthConstants.ApplicationWorth.FumingPotatoBook;
				totalValue += value;

				item.Calculation.Add(new NetworthCalculation {
					Id = "FUMING_POTATO_BOOK",
					Type = "FUMING_POTATO_BOOK",
					Value = value,
					Count = fumingPotatoBookCount
				});
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
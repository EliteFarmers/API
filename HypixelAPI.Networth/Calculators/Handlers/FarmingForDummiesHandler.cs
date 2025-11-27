using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class FarmingForDummiesHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("farming_for_dummies_count", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("farming_for_dummies_count", out var countObj)) {
			return 0;
		}

		var count = AttributeHelper.ToInt32(countObj);

		if (prices.TryGetValue("FARMING_FOR_DUMMIES", out var price)) {
			var value = price * count * NetworthConstants.ApplicationWorth.FarmingForDummies;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "FARMING_FOR_DUMMIES",
				Type = "FARMING_FOR_DUMMIES",
				Value = value,
				Count = count
			});

			return value;
		}

		return 0;
	}
}
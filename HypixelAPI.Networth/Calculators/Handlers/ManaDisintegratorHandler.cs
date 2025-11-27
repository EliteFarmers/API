using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class ManaDisintegratorHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("mana_disintegrator_count", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("mana_disintegrator_count", out var countObj)) {
			return 0;
		}

		var count = AttributeHelper.ToInt32(countObj);

		if (prices.TryGetValue("MANA_DISINTEGRATOR", out var price)) {
			var value = price * count * NetworthConstants.ApplicationWorth.ManaDisintegrator;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "MANA_DISINTEGRATOR",
				Type = "MANA_DISINTEGRATOR",
				Value = value,
				Count = count
			});

			return value;
		}

		return 0;
	}
}
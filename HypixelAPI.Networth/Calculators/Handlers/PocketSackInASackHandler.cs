using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PocketSackInASackHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("sack_pss", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("sack_pss", out var countObj)) {
			return new NetworthCalculationData();
		}

		var count = AttributeHelper.ToInt32(countObj);

		if (prices.TryGetValue("POCKET_SACK_IN_A_SACK", out var price)) {
			var value = price * count * NetworthConstants.ApplicationWorth.PocketSackInASack;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "POCKET_SACK_IN_A_SACK",
				Type = "POCKET_SACK_IN_A_SACK",
				Value = value,
				Count = count
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
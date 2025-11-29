using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EtherwarpConduitHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("ethermerge", out var ethermerge) &&
		       AttributeHelper.ToInt32(ethermerge) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("ethermerge", out var ethermergeObj)) {
			return new NetworthCalculationData();
		}

		if (prices.TryGetValue("ETHERWARP_CONDUIT", out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.Etherwarp;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "ETHERWARP_CONDUIT",
				Type = "ETHERWARP_CONDUIT",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
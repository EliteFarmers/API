using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EtherwarpConduitHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("ethermerge", out var merge))
			return false;

		var mergeStr = merge.ToString();
		if (string.IsNullOrEmpty(mergeStr)) return false;

		if (bool.TryParse(mergeStr, out var b)) return b;
		if (int.TryParse(mergeStr, out var i)) return i != 0;

		return true;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (prices.TryGetValue("ETHERWARP_CONDUIT", out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.Etherwarp;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "ETHERWARP_CONDUIT",
				Type = "ETHERWARP_CONDUIT",
				Value = value,
				Count = 1
			});

			return value;
		}

		return 0;
	}
}
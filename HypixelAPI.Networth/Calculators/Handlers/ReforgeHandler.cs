using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class ReforgeHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("modifier", out var modifier) &&
		       !string.IsNullOrEmpty(modifier.ToString()) &&
		       item.SkyblockId != null;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("modifier", out var modifierObj)) {
			return 0;
		}

		var modifier = modifierObj.ToString();
		if (string.IsNullOrEmpty(modifier)) return 0;

		if (NetworthConstants.Reforges.TryGetValue(modifier, out var reforgeItem)) {
			if (prices.TryGetValue(reforgeItem, out var price)) {
				var value = price * NetworthConstants.ApplicationWorth.Reforge;

				item.Calculation ??= new List<NetworthCalculation>();
				item.Calculation.Add(new NetworthCalculation {
					Id = reforgeItem,
					Type = "REFORGE",
					Value = value,
					Count = 1
				});

				item.Price += value;
				return value;
			}
		}

		return 0;
	}
}
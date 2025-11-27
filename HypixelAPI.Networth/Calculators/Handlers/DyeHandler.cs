using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class DyeHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("dye_item", out var dye) &&
		       !string.IsNullOrEmpty(dye.ToString());
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("dye_item", out var dyeObj)) {
			return 0;
		}

		var dye = dyeObj.ToString();
		if (string.IsNullOrEmpty(dye)) return 0;

		if (prices.TryGetValue(dye.ToUpper(), out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.Dye;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = dye,
				Type = "DYE",
				Value = value,
				Count = 1
			});

			return value;
		}

		return 0;
	}
}
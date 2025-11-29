using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class DrillPartsHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null && (
			item.Attributes.Extra.ContainsKey("drill_part_upgrade_module") ||
			item.Attributes.Extra.ContainsKey("drill_part_engine") ||
			item.Attributes.Extra.ContainsKey("drill_part_fuel_tank")
		);
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null) return new NetworthCalculationData();

		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		var parts = new[] { "drill_part_upgrade_module", "drill_part_engine", "drill_part_fuel_tank" };

		foreach (var partKey in parts) {
			if (item.Attributes.Extra.TryGetValue(partKey, out var partObj)) {
				var partId = partObj.ToString();
				if (string.IsNullOrEmpty(partId)) continue;

				if (prices.TryGetValue(partId.ToUpper(), out var price)) {
					var value = price * NetworthConstants.ApplicationWorth.DrillPart;
					totalValue += value;

					item.Calculation.Add(new NetworthCalculation {
						Id = partId.ToUpper(),
						Type = "DRILL_PART",
						Value = value,
						Count = 1
					});
				}
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
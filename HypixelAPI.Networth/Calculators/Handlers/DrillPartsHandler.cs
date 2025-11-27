using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class DrillPartsHandler : IItemNetworthHandler
{
	private static readonly string[] DrillPartTypes =
		["drill_part_upgrade_module", "drill_part_fuel_tank", "drill_part_engine"];

	public bool Applies(NetworthItem item) {
		if (item.Attributes?.Extra == null) return false;
		foreach (var type in DrillPartTypes) {
			if (item.Attributes.Extra.ContainsKey(type)) return true;
		}

		return false;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null) return 0;

		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		foreach (var type in DrillPartTypes) {
			if (item.Attributes.Extra.TryGetValue(type, out var partObj)) {
				var part = partObj.ToString();
				if (string.IsNullOrEmpty(part)) continue;

				if (prices.TryGetValue(part.ToUpper(), out var price)) {
					var value = price * NetworthConstants.ApplicationWorth.DrillPart;
					totalValue += value;

					item.Calculation.Add(new NetworthCalculation {
						Id = part.ToUpper(),
						Type = "DRILL_PART",
						Value = value,
						Count = 1
					});
				}
			}
		}

		return totalValue;
	}
}
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class RodPartsHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Hook != null || item.Attributes?.Line != null || item.Attributes?.Sinker != null;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		var parts = new[] { item.Attributes?.Hook, item.Attributes?.Line, item.Attributes?.Sinker };

		foreach (var part in parts) {
			if (part != null && !string.IsNullOrEmpty(part.Part)) {
				// Assume not soulbound for now

				if (prices.TryGetValue(part.Part.ToUpper(), out var price)) {
					var value = price * NetworthConstants.ApplicationWorth.RodPart;
					totalValue += value;

					item.Calculation.Add(new NetworthCalculation {
						Id = part.Part.ToUpper(),
						Type = "ROD_PART",
						Value = value,
						Count = 1
					});
				}
			}
		}

		return totalValue;
	}
}
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PrestigeHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId != null && NetworthConstants.Prestiges.ContainsKey(item.SkyblockId);
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.SkyblockId == null || !NetworthConstants.Prestiges.TryGetValue(item.SkyblockId, out var prestiges)) {
			return new NetworthCalculationData();
		}

		// If the item itself has a price, we don't need to calculate prestige cost (handled in base price)
		if (prices.ContainsKey(item.SkyblockId)) return new NetworthCalculationData();

		foreach (var prestigeItem in prestiges) {
			if (item.SkyblockId != null && NetworthConstants.Prestiges.TryGetValue(item.SkyblockId, out var tierList)) {
				// Calculate upgrade costs using item data if available
				if (prices.TryGetValue(prestigeItem, out var price)) {
					item.Calculation ??= new List<NetworthCalculation>();
					item.Calculation.Add(new NetworthCalculation {
						Id = prestigeItem,
						Type = "BASE_PRESTIGE_ITEM",
						Value = price,
						Count = 1
					});

					return new NetworthCalculationData { Value = price };
				}
			}
		}

		return new NetworthCalculationData();
	}
}
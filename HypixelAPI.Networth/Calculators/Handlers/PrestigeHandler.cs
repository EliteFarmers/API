using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PrestigeHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId != null && NetworthConstants.Prestiges.ContainsKey(item.SkyblockId);
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.SkyblockId == null || !NetworthConstants.Prestiges.TryGetValue(item.SkyblockId, out var prestiges)) {
			return 0;
		}

		// If the item itself has a price, we don't need to calculate prestige cost (handled in base price)
		if (prices.ContainsKey(item.SkyblockId)) return 0;

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

					item.Price += price;
					return price;
				}
			}
		}

		return 0;
	}
}
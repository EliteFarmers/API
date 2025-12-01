using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EnchantedBookHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId == "ENCHANTED_BOOK" && item.Enchantments != null &&
		       item.Enchantments.Count > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Enchantments == null) return new NetworthCalculationData();

		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		foreach (var enchant in item.Enchantments) {
			var id = enchant.Key.ToUpper();
			var level = enchant.Value;

			var enchantId = $"ENCHANTMENT_{id}_{level}";
			if (prices.TryGetValue(enchantId, out var price)) {
				var value = price * NetworthConstants.ApplicationWorth.Enchantments;
				totalValue += value;

				item.Calculation.Add(new NetworthCalculation {
					Id = enchantId,
					Type = "ENCHANTED_BOOK_ENCHANT",
					Value = value,
					Count = 1
				});
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
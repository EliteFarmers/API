using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using System.Globalization;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EnchantedBookHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item is { SkyblockId: "ENCHANTED_BOOK", Enchantments.Count: > 0 };
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Enchantments == null) return 0;

		var isSingleEnchantBook = item.Enchantments.Count == 1;
		var enchantmentPrice = 0.0;

		item.Calculation ??= new List<NetworthCalculation>();

		foreach (var enchant in item.Enchantments) {
			var name = enchant.Key;
			var value = enchant.Value;
			var priceKey = $"ENCHANTMENT_{name.ToUpper()}_{value}";

			if (prices.TryGetValue(priceKey, out var price)) {
				var finalPrice = price * (isSingleEnchantBook ? 1 : NetworthConstants.ApplicationWorth.Enchantments);

				item.Calculation.Add(new NetworthCalculation {
					Id = $"{name}_{value}".ToUpper(),
					Type = "ENCHANT",
					Value = finalPrice,
					Count = 1
				});

				enchantmentPrice += finalPrice;

				if (isSingleEnchantBook) {
					if (NetworthConstants.SpecialEnchantmentNames.TryGetValue(name.ToLower(), out var specialName)) {
						item.Name = specialName;
					}
					else {
						item.Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.Replace("_", " ").ToLower());
					}
				}
			}
		}

		if (enchantmentPrice > 0) {
			item.BasePrice = enchantmentPrice;
			item.Price += enchantmentPrice;
			return enchantmentPrice;
		}

		return 0;
	}
}
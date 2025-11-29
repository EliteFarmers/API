using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Helpers;

public static class EssenceStarsHelper
{
	public static NetworthCalculation? StarCost(Dictionary<string, double> prices, NetworthItemUpgradeCost upgrade,
		int? star = null) {
		var isEssence = upgrade.Type == "ESSENCE";
		var priceKey = isEssence ? $"ESSENCE_{upgrade.ItemId}" : upgrade.ItemId;

		if (priceKey == null || !prices.TryGetValue(priceKey, out var price)) return null;

		var calculationData = new NetworthCalculation {
			Id = priceKey,
			Type = star.HasValue ? "STAR" : "PRESTIGE",
			Value = upgrade.Amount * price * (isEssence ? NetworthConstants.ApplicationWorth.Essence : 1),
			Count = upgrade.Amount
		};

		return calculationData;
	}

	public static double StarCosts(Dictionary<string, double> prices, List<NetworthCalculation> calculation,
		List<List<NetworthItemUpgradeCost>> upgrades, string? prestigeItem = null, int? maxStars = null) {
		var price = 0.0;
		var star = 0;
		var datas = new List<NetworthCalculation?>();

		foreach (var upgradeList in upgrades) {
			if (maxStars.HasValue && star >= maxStars.Value) break;
			star++;
			foreach (var cost in upgradeList) {
				var data = StarCost(prices, cost, star);
				datas.Add(data);
				if (prestigeItem == null && data != null) {
					price += data.Value;
					calculation.Add(data);
				}
			}
		}

		if (prestigeItem != null && datas.Count > 0 && datas[0] != null) {
			var prestige = datas[0]!.Type == "PRESTIGE";
			var totalPrestigePrice = datas.Where(d => d != null).Sum(d => d!.Value);

			var calculationData = new NetworthCalculation {
				Id = prestigeItem,
				Type = prestige ? "PRESTIGE" : "STARS",
				Value = totalPrestigePrice,
				Count = prestige ? 1 : star
			};

			price += calculationData.Value;
			calculation.Add(calculationData);
		}

		return price;
	}
}
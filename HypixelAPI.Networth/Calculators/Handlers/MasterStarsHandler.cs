using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class MasterStarsHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		if (item.Attributes?.Extra == null) return false;

		if (item.Attributes.Extra.TryGetValue("dungeon_item_level", out var level)) {
			return AttributeHelper.ToInt32(level) > 5;
		}

		if (item.Attributes.Extra.TryGetValue("upgrade_level", out var upgradeLevel)) {
			return AttributeHelper.ToInt32(upgradeLevel) > 5;
		}

		return false;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null) return new NetworthCalculationData();

		var level = 0;
		if (item.Attributes.Extra.TryGetValue("dungeon_item_level", out var levelObj)) {
			level = AttributeHelper.ToInt32(levelObj);
		} else if (item.Attributes.Extra.TryGetValue("upgrade_level", out var upgradeLevelObj)) {
			level = AttributeHelper.ToInt32(upgradeLevelObj);
		}

		if (level <= 5) return new NetworthCalculationData();

		var masterStars = level - 5;
		var totalValue = 0.0;

		item.Calculation ??= new List<NetworthCalculation>();

		for (var i = 1; i <= masterStars; i++) {
			var starItem = NetworthConstants.MasterStars[i - 1];
			if (prices.TryGetValue(starItem, out var price)) {
				var value = price * NetworthConstants.ApplicationWorth.MasterStar;
				totalValue += value;

				item.Calculation.Add(new NetworthCalculation {
					Id = starItem,
					Type = "MASTER_STAR",
					Value = value,
					Count = 1
				});
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
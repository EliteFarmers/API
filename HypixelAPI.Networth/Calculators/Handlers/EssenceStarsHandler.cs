using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EssenceStarsHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		if (item.UpgradeCosts == null || item.UpgradeCosts.Count == 0) return false;

		if (item.Attributes?.Extra != null) {
			if (item.Attributes.Extra.TryGetValue("dungeon_item_level", out var levelObj)) {
				return AttributeHelper.ToInt32(levelObj) > 0;
			}
			if (item.Attributes.Extra.TryGetValue("upgrade_level", out var upgradeLevelObj)) {
				return AttributeHelper.ToInt32(upgradeLevelObj) > 0;
			}
		}
		
		return false;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.UpgradeCosts == null) return new NetworthCalculationData();

		item.Calculation ??= new List<NetworthCalculation>();

		int? maxStars = null;
		if (item.Attributes?.Extra != null) {
			if (item.Attributes.Extra.TryGetValue("dungeon_item_level", out var levelObj)) {
				maxStars = AttributeHelper.ToInt32(levelObj);
			} else if (item.Attributes.Extra.TryGetValue("upgrade_level", out var upgradeLevelObj)) {
				maxStars = AttributeHelper.ToInt32(upgradeLevelObj);
			}
		}
		Console.WriteLine($"[DEBUG] EssenceStars: MaxStars={maxStars}, UpgradeCostsCount={item.UpgradeCosts.Count}");

		var value = EssenceStarsHelper.StarCosts(prices, item.Calculation, item.UpgradeCosts, maxStars: maxStars);

		return new NetworthCalculationData { Value = value };
	}
}
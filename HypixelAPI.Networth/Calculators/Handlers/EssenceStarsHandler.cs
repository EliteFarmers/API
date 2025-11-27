using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Models;
using System.Text.RegularExpressions;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EssenceStarsHandler : IItemNetworthHandler
{
	private int GetUpgradeLevel(NetworthItem item) {
		var dungeonItemLevel = 0;
		if (item.Attributes?.Extra != null &&
		    item.Attributes.Extra.TryGetValue("dungeon_item_level", out var dungeonLevelObj)) {
			int.TryParse(Regex.Replace(dungeonLevelObj.ToString() ?? "0", @"\D", ""), out dungeonItemLevel);
		}

		var upgradeLevel = 0;
		if (item.Attributes?.Extra != null &&
		    item.Attributes.Extra.TryGetValue("upgrade_level", out var upgradeLevelObj)) {
			int.TryParse(Regex.Replace(upgradeLevelObj.ToString() ?? "0", @"\D", ""), out upgradeLevel);
		}

		return Math.Max(dungeonItemLevel, upgradeLevel);
	}

	public bool Applies(NetworthItem item) {
		return item.UpgradeCosts != null && item.UpgradeCosts.Count > 0 && GetUpgradeLevel(item) > 0;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		var level = GetUpgradeLevel(item);
		item.Calculation ??= new List<NetworthCalculation>();

		// Take slice of upgrade costs up to level
		var costsToCalculate = item.UpgradeCosts!.Take(level).ToList();

		return EssenceStarsHelper.StarCosts(prices, item.Calculation, costsToCalculate);
	}
}
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using System.Text.RegularExpressions;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class MasterStarsHandler : IItemNetworthHandler
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
		return item.UpgradeCosts != null && GetUpgradeLevel(item) > 5;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		var level = GetUpgradeLevel(item);
		var starsUsed = Math.Min(level - 5, 5);
		var totalValue = 0.0;

		item.Calculation ??= new List<NetworthCalculation>();

		if (item.UpgradeCosts!.Count <= 5) {
			for (var i = 0; i < starsUsed; i++) {
				var starId = NetworthConstants.MasterStars[i];
				if (prices.TryGetValue(starId, out var price)) {
					var value = price * NetworthConstants.ApplicationWorth.MasterStar;
					totalValue += value;

					item.Calculation.Add(new NetworthCalculation {
						Id = starId,
						Type = "MASTER_STAR",
						Value = value,
						Count = 1
					});
				}
			}
		}

		return totalValue;
	}
}
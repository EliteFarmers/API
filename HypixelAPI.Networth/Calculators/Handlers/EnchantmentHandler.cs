using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EnchantmentHandler : IItemNetworthHandler
{
	private static readonly Dictionary<string, (string UpgradeItem, int Tier)> EnchantmentUpgrades = new() {
		{ "SCAVENGER", ("GOLDEN_BOUNTY", 6) },
		{ "PESTERMINATOR", ("PESTHUNTING_GUIDE", 6) },
		{ "LUCK_OF_THE_SEA", ("GOLD_BOTTLE_CAP", 7) },
		{ "PISCARY", ("TROUBLED_BUBBLE", 7) },
		{ "FRAIL", ("SEVERED_PINCER", 7) },
		{ "SPIKED_HOOK", ("OCTOPUS_TENDRIL", 7) },
		{ "CHARM", ("CHAIN_END_TIMES", 6) },
		{ "VENOMOUS", ("FATEFUL_STINGER", 7) }
	};

	public bool Applies(NetworthItem item) {
		return item.SkyblockId != "ENCHANTED_BOOK" && item.Enchantments is { Count: > 0 };
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		double totalValue = 0;

		if (item.Enchantments == null) return new NetworthCalculationData();

		foreach (var (name, level) in item.Enchantments) {
			var upperName = name.ToUpper();
			var currentLevel = level;

			// Check blocked enchantments
			if (item.SkyblockId != null &&
			    NetworthConstants.BlockedEnchantments.TryGetValue(item.SkyblockId, out var blocked) &&
			    blocked.Contains(upperName)) {
				continue;
			}

			// Check ignored enchantments
			if (NetworthConstants.IgnoredEnchantments.TryGetValue(upperName, out var ignoredLevel) &&
			    ignoredLevel == currentLevel) {
				continue;
			}

			// Stacking enchantments
			if (NetworthConstants.StackingEnchantments.Contains(upperName)) {
				currentLevel = 1;
			}

			// Silex logic
			if (upperName == "EFFICIENCY" && currentLevel >= 6 &&
			    (item.SkyblockId == null || !NetworthConstants.IgnoreSilex.Contains(item.SkyblockId))) {
				var efficiencyLevel = currentLevel - (item.SkyblockId == "STONK_PICKAXE" ? 6 : 5);
				if (efficiencyLevel > 0) {
					if (prices.TryGetValue("SIL_EX", out var silexPrice)) {
						var silexValue = silexPrice * efficiencyLevel * NetworthConstants.ApplicationWorth.Silex;
						totalValue += silexValue;

						item.Calculation ??= new List<NetworthCalculation>();
						item.Calculation.Add(new NetworthCalculation {
							Id = "SIL_EX",
							Type = "SILEX",
							Value = silexValue,
							Count = efficiencyLevel
						});
					}
				}
			}

			// Enchantment Upgrades
			if (EnchantmentUpgrades.TryGetValue(upperName, out var upgrade) && currentLevel >= upgrade.Tier) {
				if (prices.TryGetValue(upgrade.UpgradeItem, out var upgradePrice)) {
					var upgradeValue = upgradePrice * NetworthConstants.ApplicationWorth.EnchantmentUpgrades;
					totalValue += upgradeValue;

					item.Calculation ??= new List<NetworthCalculation>();
					item.Calculation.Add(new NetworthCalculation {
						Id = upgrade.UpgradeItem,
						Type = "ENCHANTMENT_UPGRADE",
						Value = upgradeValue,
						Count = 1
					});
				}
			}

			// Standard Enchantment Value
			var enchantKey = $"ENCHANTMENT_{upperName}_{currentLevel}";
			if (prices.TryGetValue(enchantKey, out var enchantPrice)) {
				var worthMultiplier =
					NetworthConstants.EnchantmentsWorth.GetValueOrDefault(upperName,
						NetworthConstants.ApplicationWorth.Enchantments);

				var enchantValue = enchantPrice * worthMultiplier;
				totalValue += enchantValue;

				item.Calculation ??= new List<NetworthCalculation>();
				item.Calculation.Add(new NetworthCalculation {
					Id = enchantKey,
					Type = "ENCHANT",
					Value = enchantValue,
					Count = 1
				});
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using SkyblockRepo;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class RecombobulatorHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		var hasEnchantments = item.Enchantments is { Count: > 0 };

		// Check against allowed IDs and basic accessory detection from lore

		var allowsRecomb = NetworthConstants.AllowedRecombobulatedIds.Contains(item.SkyblockId ?? "");

		// Try to get category from SkyblockRepo if available
		var isAllowedCategory = false;
		if (item.SkyblockId != null) {
			try {
				var repoItem = SkyblockRepoClient.Data.Items.GetValueOrDefault(item.SkyblockId);
				if (repoItem is { Category: not null }) {
					isAllowedCategory = NetworthConstants.AllowedRecombobulatedCategories.Contains(repoItem.Category);
				}
			}
			catch {
				// SkyblockRepo might not be initialized, fall back to lore check
			}
		}

		// Fallback: Basic accessory check from lore if available
		var isAccessory = false;
		if (!isAllowedCategory && item.Lore != null && item.Lore.Count > 0) {
			var lastLine = item.Lore.Last();
			isAccessory = lastLine.Contains("ACCESSORY") || lastLine.Contains("HATCESSORY");
		}

		// Check for recombobulated property (rarity upgrade)
		var isRecombobulated = item.Attributes?.Extra != null &&
		                       item.Attributes.Extra.TryGetValue("rarity_upgrades", out var rarityUpgrades) &&
		                       AttributeHelper.ToInt32(rarityUpgrades) > 0 &&
		                       (!item.Attributes.Extra.TryGetValue("item_tier", out var itemTier) ||
		                        AttributeHelper.ToInt32(itemTier) == 0);

		return isRecombobulated && (hasEnchantments || allowsRecomb || isAllowedCategory || isAccessory);
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		var recombobulatorApplicationWorth = item.SkyblockId == "BONE_BOOMERANG"
			? NetworthConstants.ApplicationWorth.Recombobulator * 0.5
			: NetworthConstants.ApplicationWorth.Recombobulator;

		if (prices.TryGetValue("RECOMBOBULATOR_3000", out var price)) {
			var value = price * recombobulatorApplicationWorth;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "RECOMBOBULATOR_3000",
				Type = "RECOMBOBULATOR_3000",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
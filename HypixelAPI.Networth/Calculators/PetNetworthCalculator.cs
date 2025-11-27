using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using System.Globalization;

namespace HypixelAPI.Networth.Calculators;

public class PetNetworthCalculator
{
	private readonly List<IItemNetworthHandler> _handlers;

	public PetNetworthCalculator() {
		_handlers = new List<IItemNetworthHandler> {
			new PetItemHandler(),
			new SoulboundPetSkinHandler(),
			new PetCandyHandler() // Must be last
		};
	}

	public async Task<NetworthResult> CalculateAsync(NetworthItem item, Dictionary<string, double> prices) {
		if (item.PetInfo == null) return new NetworthResult { Item = item, Networth = 0 };

		// Calculate base price
		var basePrice = GetBasePrice(item, prices);
		if (basePrice == null) {
			return new NetworthResult { Item = item, Networth = 0 };
		}

		item.BasePrice = basePrice.Value;
		item.Price = item.BasePrice;
		item.Calculation ??= new List<NetworthCalculation>();

		// Apply handlers
		foreach (var handler in _handlers) {
			if (handler.Applies(item)) {
				handler.Calculate(item, prices);
			}
		}

		return new NetworthResult {
			Item = item,
			Price = item.Price,
			BasePrice = item.BasePrice,
			Networth = item.Price, // Assuming Count is 1 for pets usually, or use item.Count
			Calculation = item.Calculation
		};
	}

	private double? GetBasePrice(NetworthItem item, Dictionary<string, double> prices) {
		var petInfo = item.PetInfo!;
		var tier = petInfo.Tier;
		var type = petInfo.Type;
		var skin = petInfo.Skin;

		var basePetId = $"{tier}_{type}";
		var petId = $"{basePetId}{(string.IsNullOrEmpty(skin) ? "" : $"_SKINNED_{skin}")}";

		var levelData = GetPetLevel(petInfo);
		var level = levelData.Level;
		var xpMax = levelData.XpMax;
		var xp = levelData.Xp;

		var lvl1Price = prices.GetValueOrDefault($"LVL_1_{basePetId}", 0);
		var lvl100Price = prices.GetValueOrDefault($"LVL_100_{basePetId}", 0);
		var lvl200Price = prices.GetValueOrDefault($"LVL_200_{basePetId}", 0);

		if (!string.IsNullOrEmpty(skin)) {
			lvl1Price = Math.Max(prices.GetValueOrDefault($"LVL_1_{petId}", 0), lvl1Price);
			lvl100Price = Math.Max(prices.GetValueOrDefault($"LVL_100_{petId}", 0), lvl100Price);
			lvl200Price = Math.Max(prices.GetValueOrDefault($"LVL_200_{petId}", 0), lvl200Price);
		}

		if (lvl1Price == 0 && lvl100Price == 0 && lvl200Price == 0) {
			return null;
		}

		var basePrice = lvl200Price > 0 ? lvl200Price : lvl100Price;

		if (level < 100 && xpMax > 0) {
			var baseFormula = (lvl100Price - lvl1Price) / (double)xpMax;
			if (baseFormula != 0) {
				basePrice = baseFormula * (double)xp + lvl1Price;
			}
		}
		else if (level is > 100 and < 200) {
			var levelDiff = level - 100;
			if (levelDiff != 1) {
				var baseFormula = (lvl200Price - lvl100Price) / 100.0;
				if (baseFormula != 0) {
					basePrice = baseFormula * levelDiff + lvl100Price;
				}
			}
		}

		return basePrice;
	}

	private (int Level, double XpMax, double Xp) GetPetLevel(NetworthItemPetInfo petInfo) {
		var maxPetLevel = NetworthConstants.SpecialLevels.GetValueOrDefault(petInfo.Type, 100);

		// Calculate offset
		var tierName = petInfo.Tier;
		// Check for tier boost
		if (petInfo.HeldItem == "PET_ITEM_TIER_BOOST") {
			var tierIndex = NetworthConstants.Tiers.IndexOf(tierName);
			if (tierIndex != -1 && tierIndex < NetworthConstants.Tiers.Count - 1) {
				tierName = NetworthConstants.Tiers[tierIndex + 1];
			}
		}

		var rarityKey = petInfo.Type == "BINGO" ? "COMMON" : tierName;
		var petOffset = NetworthConstants.RarityOffset.GetValueOrDefault(rarityKey, 0);

		// Slice levels
		var petLevels = NetworthConstants.Levels.Skip(petOffset).Take(maxPetLevel - 1).ToList();

		var level = 1;
		var totalExp = 0.0;
		var xp = (double)petInfo.Exp;

		foreach (var levelXp in petLevels) {
			totalExp += levelXp;
			if (totalExp > xp) {
				totalExp -= levelXp;
				break;
			}

			level++;
		}

		var xpMax = petLevels.Sum();

		return (Math.Min(level, maxPetLevel), xpMax, xp);
	}
}
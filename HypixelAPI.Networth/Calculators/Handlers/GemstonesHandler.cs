using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class GemstonesHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Gems != null && item.Gems.Count > 0 && item.GemstoneSlots != null && item.GemstoneSlots.Count > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		var unlockedSlots = item.UnlockedSlots ?? new List<string>();
		if (item.Gems != null && item.UnlockedSlots == null) {
			foreach (var kvp in item.Gems) {
				if (kvp.Value == null) {
					unlockedSlots.Add(kvp.Key);
				}
			}
		}

		var isDivansArmor = item.SkyblockId != null && (item.SkyblockId.StartsWith("DIVAN_") ||
		                                                item.SkyblockId == "DIVAN_HELMET" ||
		                                                item.SkyblockId == "DIVAN_CHESTPLATE" ||
		                                                item.SkyblockId == "DIVAN_LEGGINGS" ||
		                                                item.SkyblockId == "DIVAN_BOOTS");
		var isCrimsonArmor = item.SkyblockId != null && (
			item.SkyblockId.Contains("AURORA") || item.SkyblockId.Contains("CRIMSON") ||
			item.SkyblockId.Contains("TERROR") || item.SkyblockId.Contains("HOLLOW") ||
			item.SkyblockId.Contains("FERVOR")
		) && (item.SkyblockId.EndsWith("_HELMET") || item.SkyblockId.EndsWith("_CHESTPLATE") ||
		      item.SkyblockId.EndsWith("_LEGGINGS") || item.SkyblockId.EndsWith("_BOOTS"));

		if (isDivansArmor || isCrimsonArmor) {
			var application = isDivansArmor
				? NetworthConstants.ApplicationWorth.GemstoneChambers
				: NetworthConstants.ApplicationWorth.GemstoneSlots;

			foreach (var unlockedSlot in unlockedSlots) {
				// Strip index from unlockedSlot (e.g. "AMBER_0" -> "AMBER")
				var slotType = unlockedSlot.Split('_')[0];
				var slot = item.GemstoneSlots?.FirstOrDefault(s => s.SlotType == slotType);
				if (slot != null && slot.Costs != null) {
					var slotCost = 0.0;
					foreach (var cost in slot.Costs) {
						if (cost.Type == "COINS") {
							slotCost += cost.Coins ?? 0;
						}
						else if (cost.Type == "ITEM" && cost.ItemId != null) {
							if (prices.TryGetValue(cost.ItemId, out var price)) {
								slotCost += price * cost.Amount;
							}
						}
					}

					var value = slotCost * application;
					totalValue += value;
					item.Calculation.Add(new NetworthCalculation {
						Id = slot.SlotType,
						Type = "GEMSTONE_SLOT",
						Value = value,
						Count = 1
					});
				}
			}
		}

		// Calculate Gemstones Value
		if (item.Gems != null) {
			foreach (var kvp in item.Gems) {
				var key = kvp.Key;
				var quality = kvp.Value;

				if (quality == null || key.EndsWith("_gem")) continue;

				// Find the gem type
				// It should be in `${key}_gem`
				var gemTypeKey = $"{key}_gem";
				string? gemType = null;
				if (item.Gems.TryGetValue(gemTypeKey, out var typeVal)) {
					gemType = typeVal;
				}

				if (gemType == null) {
					// Try to guess from key if not explicitly found
					var parts = key.Split('_');
					if (parts.Length > 1) {
						var potentialType = string.Join("_", parts.Take(parts.Length - 1));
						if (!NetworthConstants.GemstoneSlots.Contains(potentialType)) {
							gemType = potentialType;
						}
					}
				}

				if (gemType != null) {
					var gemId = $"{quality}_{gemType}_GEM".ToUpper();
					if (prices.TryGetValue(gemId, out var price)) {
						var value = price * NetworthConstants.ApplicationWorth.Gemstone;
						totalValue += value;
						item.Calculation.Add(new NetworthCalculation {
							Id = gemId,
							Type = "GEMSTONE",
							Value = value,
							Count = 1
						});
					}
				}
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
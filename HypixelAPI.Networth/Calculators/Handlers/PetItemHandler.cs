using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PetItemHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.PetInfo != null && !string.IsNullOrEmpty(item.PetInfo.HeldItem);
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.PetInfo?.HeldItem == null) return new NetworthCalculationData();

		var heldItem = item.PetInfo.HeldItem;

		if (prices.TryGetValue(heldItem, out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.PetItem;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = heldItem,
				Type = "PET_ITEM",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
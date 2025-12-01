using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class SoulboundPetSkinHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.PetInfo != null &&
		       !string.IsNullOrEmpty(item.PetInfo.Skin) &&
		       item.IsSoulbound;
		// && !item.nonCosmetic
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.PetInfo?.Skin == null) return new NetworthCalculationData();

		var skin = item.PetInfo.Skin;
		var priceKey = $"PET_SKIN_{skin}";

		if (prices.TryGetValue(priceKey, out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.SoulboundPetSkins;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = skin,
				Type = "SOULBOUND_PET_SKIN",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value, IsCosmetic = true };
		}

		return new NetworthCalculationData();
	}
}
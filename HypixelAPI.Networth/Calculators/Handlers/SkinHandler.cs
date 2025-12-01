using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class SkinHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("skin", out var skin) &&
		       !string.IsNullOrEmpty(skin.ToString()) &&
		       !item.IsSoulbound;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("skin", out var skinObj)) {
			return new NetworthCalculationData();
		}

		var skin = skinObj.ToString();
		if (string.IsNullOrEmpty(skin) || item.SkyblockId == null) return new NetworthCalculationData();

		var skinnedId = $"{item.SkyblockId}_SKINNED_{skin}";

		if (prices.TryGetValue(skinnedId, out var skinnedPrice)) {
			var basePrice = prices.GetValueOrDefault(item.SkyblockId, 0);
			
			// Only apply if the skinned version is worth more than the base version
			if (skinnedPrice > basePrice) {
				var value = skinnedPrice - basePrice;

				item.Calculation ??= new List<NetworthCalculation>();
				item.Calculation.Add(new NetworthCalculation {
					Id = skin,
					Type = "SKIN",
					Value = value,
					Count = 1
				});

				return new NetworthCalculationData { Value = value, IsCosmetic = true };
			}
		}

		return new NetworthCalculationData();
	}
}

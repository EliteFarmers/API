using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class SoulboundSkinHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("skin", out var skin) &&
		       !string.IsNullOrEmpty(skin.ToString()) &&
		       item.IsSoulbound;
		// && !item.nonCosmetic
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("skin", out var skinObj)) {
			return new NetworthCalculationData();
		}

		var skin = skinObj.ToString();
		if (string.IsNullOrEmpty(skin)) return new NetworthCalculationData();

		if (prices.TryGetValue(skin, out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.SoulboundSkins;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = skin,
				Type = "SOULBOUND_SKIN",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value, IsCosmetic = true };
		}

		return new NetworthCalculationData();
	}
}
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

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("skin", out var skinObj)) {
			return 0;
		}

		var skin = skinObj.ToString();
		if (string.IsNullOrEmpty(skin)) return 0;

		if (prices.TryGetValue(skin, out var price)) {
			var value = price * NetworthConstants.ApplicationWorth.SoulboundSkins;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = skin,
				Type = "SOULBOUND_SKIN",
				Value = value,
				Count = 1
			});

			item.Price += value;
			return value;
		}

		return 0;
	}
}
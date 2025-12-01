using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class ArtOfWarHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("art_of_war_count", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("art_of_war_count", out var countObj)) {
			return new NetworthCalculationData();
		}

		var count = AttributeHelper.ToInt32(countObj);

		if (prices.TryGetValue("THE_ART_OF_WAR", out var price)) {
			var value = price * count * NetworthConstants.ApplicationWorth.ArtOfWar;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "THE_ART_OF_WAR",
				Type = "THE_ART_OF_WAR",
				Value = value,
				Count = count
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
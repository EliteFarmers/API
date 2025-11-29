using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PickonimbusHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId == "PICKONIMBUS" &&
		       item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("pickonimbus_durability", out var durability) &&
		       AttributeHelper.ToInt32(durability) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("pickonimbus_durability", out var durabilityObj)) {
			return new NetworthCalculationData();
		}

		var durability = AttributeHelper.ToInt32(durabilityObj);

		if (item.BasePrice > 0) {
			var value = (double)item.BasePrice / 5000 * durability - item.BasePrice;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "PICKONIMBUS_DURABLITY",
				Type = "PICKONIMBUS",
				Value = value,
				Count = durability
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
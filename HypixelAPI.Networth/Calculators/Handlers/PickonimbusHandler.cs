using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PickonimbusHandler : IItemNetworthHandler
{
	private const int PickonimbusDurability = 5000;

	public bool Applies(NetworthItem item) {
		return item.SkyblockId == "PICKONIMBUS" &&
		       item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("pickonimbus_durability", out var durability) &&
		       AttributeHelper.ToInt32(durability) < PickonimbusDurability;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("pickonimbus_durability", out var durabilityObj)) {
			return 0;
		}

		var durability = AttributeHelper.ToInt32(durabilityObj);
		var reduction = (double)durability / PickonimbusDurability;
		var value = item.BasePrice * (reduction - 1);

		item.Calculation ??= new List<NetworthCalculation>();
		item.Calculation.Add(new NetworthCalculation {
			Id = "PICKONIMBUS_DURABLITY",
			Type = "PICKONIMBUS",
			Value = value,
			Count = PickonimbusDurability - durability
		});

		return value;
	}
}
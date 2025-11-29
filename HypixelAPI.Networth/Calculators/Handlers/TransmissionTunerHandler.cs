using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class TransmissionTunerHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("tuned_transmission", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("tuned_transmission", out var countObj)) {
			return new NetworthCalculationData();
		}

		var count = AttributeHelper.ToInt32(countObj);

		if (prices.TryGetValue("TRANSMISSION_TUNER", out var price)) {
			var value = price * count * NetworthConstants.ApplicationWorth.TunedTransmission;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "TRANSMISSION_TUNER",
				Type = "TRANSMISSION_TUNER",
				Value = value,
				Count = count
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
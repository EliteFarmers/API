using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PulseRingThunderHandler : IItemNetworthHandler
{
	private const int MaxThunderCharge = 5000000;

	public bool Applies(NetworthItem item) {
		return item.SkyblockId == "PULSE_RING" &&
		       item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("thunder_charge", out var charge) &&
		       AttributeHelper.ToInt32(charge) > 0;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("thunder_charge", out var chargeObj)) {
			return 0;
		}

		var charge = AttributeHelper.ToInt32(chargeObj);
		var thunderUpgrades = Math.Floor(Math.Min(charge, MaxThunderCharge) / 50_000.0);

		if (prices.TryGetValue("THUNDER_IN_A_BOTTLE", out var price)) {
			var value = price * thunderUpgrades * NetworthConstants.ApplicationWorth.ThunderInABottle;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "THUNDER_IN_A_BOTTLE",
				Type = "THUNDER_CHARGE",
				Value = value,
				Count = (int)thunderUpgrades
			});

			return value;
		}

		return 0;
	}
}
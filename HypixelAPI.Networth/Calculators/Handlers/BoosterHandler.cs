using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using System.Collections;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class BoosterHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("boosters", out var boosters) &&
		       boosters is IEnumerable;
	}

	
	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("boosters", out var boostersObj) ||
		    boostersObj is not IEnumerable boosters) {
			return 0;
		}

		var totalValue = 0.0;
		item.Calculation ??= [];

		foreach (var booster in boosters) {
			var boosterStr = booster.ToString();
			if (string.IsNullOrEmpty(boosterStr)) continue;

			var boosterId = $"{boosterStr.ToUpper()}_BOOSTER";
			if (prices.TryGetValue(boosterId, out var price)) {
				var value = price * NetworthConstants.ApplicationWorth.Booster;
				totalValue += value;

				item.Calculation.Add(new NetworthCalculation {
					Id = boosterId,
					Type = "BOOSTER",
					Value = value,
					Count = 1
				});
			}
		}

		return totalValue;
	}
}
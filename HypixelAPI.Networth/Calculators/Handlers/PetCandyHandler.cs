using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PetCandyHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.PetInfo is { CandyUsed: > 0 };
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.PetInfo is not { CandyUsed: > 0 }) return 0;

		var candyUsed = item.PetInfo.CandyUsed;

		if (item.BasePrice <= 0) return 0;
		
		var reduceValue = item.BasePrice * (1 - NetworthConstants.ApplicationWorth.PetCandy);
		if (reduceValue > 5000000) reduceValue = 5000000;
		var value = -reduceValue;

		item.Calculation ??= [];
		item.Calculation.Add(new NetworthCalculation {
			Id = "CANDY",
			Type = "PET_CANDY",
			Value = value,
			Count = candyUsed
		});

		return new NetworthCalculationData { Value = value };

	}
}
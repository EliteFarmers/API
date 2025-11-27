using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class PetCandyHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		if (item.PetInfo is not { CandyUsed: > 0 }) return false;

		// Check if pet type is blocked from candy reduction
		if (NetworthConstants.BlockedCandyReducePets.Contains(item.PetInfo.Type))
			return false;

		// Currently assume candy is being used. Need to add XP tables later
		return true;
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.PetInfo == null) return 0;

		var reduceValue = item.BasePrice * (1 - NetworthConstants.ApplicationWorth.PetCandy);

		// Max reduction logic
		// Defaulting to higher cap (5m) as we don't have pet level readily available.
		const double maxReduction = 5000000.0;

		reduceValue = Math.Min(reduceValue, maxReduction);

		item.Calculation ??= new List<NetworthCalculation>();
		item.Calculation.Add(new NetworthCalculation {
			Id = "CANDY",
			Type = "PET_CANDY",
			Value = -reduceValue,
			Count = item.PetInfo.CandyUsed
		});

		item.Price -= reduceValue;
		return -reduceValue;
	}
}
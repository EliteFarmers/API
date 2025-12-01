using HypixelAPI.Networth.Interfaces;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators;

public class ItemNetworthCalculator(IPriceProvider priceProvider)
{
	public async Task<NetworthResult> CalculateAsync(NetworthItem item) {
		var prices = await priceProvider.GetPricesAsync();

		if (item.PetInfo != null) {
			var petCalculator = new PetNetworthCalculator();
			return await petCalculator.CalculateAsync(item, prices);
		}

		var calculator = new SkyBlockItemNetworthCalculator();
		return await calculator.CalculateAsync(item, prices);
	}
}
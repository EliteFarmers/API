using HypixelAPI.Networth.Calculators;
using HypixelAPI.Networth.Interfaces;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Services;

public class NetworthService(IPriceProvider? priceProvider = null)
{
	private readonly IPriceProvider _priceProvider = priceProvider ?? new SkyHelperPriceProvider();

	public async Task<NetworthResult?> GetItemNetworthAsync(NetworthItem item) {
		await _priceProvider.GetPricesAsync();
		var calculator = new ItemNetworthCalculator(_priceProvider);
		return await calculator.CalculateAsync(item);
	}
}
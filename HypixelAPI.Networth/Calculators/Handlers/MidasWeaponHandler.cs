using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class MidasWeaponHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.SkyblockId != null && (item.SkyblockId == "MIDAS_SWORD" || item.SkyblockId == "MIDAS_STAFF" || item.SkyblockId == "STARRED_MIDAS_SWORD" || item.SkyblockId == "STARRED_MIDAS_STAFF");
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("winning_bid", out var bidObj)) {
			return new NetworthCalculationData();
		}

		var winningBid = Convert.ToDouble(bidObj);
		if (item.Attributes.Extra.TryGetValue("additional_coins", out var additionalCoinsObj)) {
			winningBid += Convert.ToDouble(additionalCoinsObj);
		}
		var maxBid = item.SkyblockId == "MIDAS_SWORD" ? 50_000_000 : 100_000_000;
		Console.WriteLine($"[DEBUG] Midas: Id={item.SkyblockId}, Bid={winningBid}, Max={maxBid}, Additional={item.Attributes.Extra.ContainsKey("additional_coins")}");

		if (winningBid >= maxBid) {
			var maxPriceId = $"{item.SkyblockId}_50M"; // or 100M
			if (item.SkyblockId == "MIDAS_STAFF" || item.SkyblockId == "STARRED_MIDAS_STAFF") maxPriceId = $"{item.SkyblockId}_100M";

			if (prices.TryGetValue(maxPriceId, out var price)) {
				item.Calculation ??= new List<NetworthCalculation>();
				item.Calculation.Add(new NetworthCalculation {
					Id = maxPriceId,
					Type = "MIDAS_WEAPON_MAX",
					Value = price,
					Count = 1
				});
				
				var valueDiff = price - item.BasePrice;
				item.BasePrice = price;
				return new NetworthCalculationData { Value = valueDiff };
			}
		}

		return new NetworthCalculationData();
	}
}
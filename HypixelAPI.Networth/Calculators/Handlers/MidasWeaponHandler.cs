using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class MidasWeaponHandler : IItemNetworthHandler
{
	private static readonly Dictionary<string, (double MaxBid, string Type)> MidasSwords = new() {
		{ "MIDAS_SWORD", (50_000_000, "MIDAS_SWORD_50M") },
		{ "STARRED_MIDAS_SWORD", (250_000_000, "STARRED_MIDAS_SWORD_250M") },
		{ "MIDAS_STAFF", (100_000_000, "MIDAS_STAFF_100M") },
		{ "STARRED_MIDAS_STAFF", (500_000_000, "STARRED_MIDAS_STAFF_500M") }
	};

	public bool Applies(NetworthItem item) {
		return item.SkyblockId != null && MidasSwords.ContainsKey(item.SkyblockId);
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.SkyblockId == null || !MidasSwords.TryGetValue(item.SkyblockId, out var midasData)) {
			return 0;
		}

		var winningBid = 0.0;
		if (item.Attributes?.Extra != null && item.Attributes.Extra.TryGetValue("winning_bid", out var bidObj)) {
			winningBid = Convert.ToDouble(bidObj);
		}

		var additionalCoins = 0.0;
		if (item.Attributes?.Extra != null && item.Attributes.Extra.TryGetValue("additional_coins", out var coinsObj)) {
			additionalCoins = Convert.ToDouble(coinsObj);
		}

		if (winningBid + additionalCoins >= midasData.MaxBid) {
			if (prices.TryGetValue(midasData.Type, out var price)) {
				item.Calculation ??= new List<NetworthCalculation>();
				item.Calculation.Add(new NetworthCalculation {
					Id = item.SkyblockId,
					Type = midasData.Type,
					Value = price,
					Count = 1
				});
				
				item.BasePrice = price;
				return 0;
			}
		}

		return 0;
	}
}
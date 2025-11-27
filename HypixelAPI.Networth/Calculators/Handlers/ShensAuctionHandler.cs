using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class ShensAuctionHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.ContainsKey("price") &&
		       item.Attributes.Extra.ContainsKey("auction") &&
		       item.Attributes.Extra.ContainsKey("bid");
	}

	public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("price", out var priceObj)) {
			return 0;
		}

		var pricePaid = Convert.ToDouble(priceObj) * NetworthConstants.ApplicationWorth.ShensAuctionPrice;

		if (pricePaid > item.BasePrice) {
			item.BasePrice = pricePaid;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = item.SkyblockId ?? "SHENS_AUCTION_ITEM",
				Type = "SHENS_AUCTION",
				Value = pricePaid,
				Count = 1
			});
		}

		return 0;
	}
}
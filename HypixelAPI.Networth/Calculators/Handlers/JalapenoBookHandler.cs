using HypixelAPI.Networth.Calculators.Helpers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class JalapenoBookHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("jalapeno_count", out var count) &&
		       AttributeHelper.ToInt32(count) > 0;
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("jalapeno_count", out var countObj)) {
			return new NetworthCalculationData();
		}

		var count = AttributeHelper.ToInt32(countObj);

		if (prices.TryGetValue("JALAPENO_BOOK", out var price)) {
			var value = price * count * NetworthConstants.ApplicationWorth.JalapenoBook;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = "JALAPENO_BOOK",
				Type = "JALAPENO_BOOK",
				Value = value,
				Count = count
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}

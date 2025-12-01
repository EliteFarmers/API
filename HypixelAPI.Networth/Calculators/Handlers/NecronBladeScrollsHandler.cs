using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using System.Text.Json;
using System.Linq;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class NecronBladeScrollsHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null && item.Attributes.Extra.ContainsKey("ability_scroll");
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null || !item.Attributes.Extra.TryGetValue("ability_scroll", out var scrollsObj)) {
			return new NetworthCalculationData();
		}

		var totalValue = 0.0;
		item.Calculation ??= new List<NetworthCalculation>();

		var scrolls = new List<string>();
		if (scrollsObj is JsonElement { ValueKind: JsonValueKind.Array } scrollsArray) {
			foreach (var scrollElement in scrollsArray.EnumerateArray()) {
				var s = scrollElement.GetString();
				if (!string.IsNullOrEmpty(s)) scrolls.Add(s);
			}
		} else if (scrollsObj is IEnumerable<string> strEnum) {
			scrolls.AddRange(strEnum);
		} else if (scrollsObj is IEnumerable<object> objEnum) {
			scrolls.AddRange(objEnum.Select(o => o.ToString()).Where(s => !string.IsNullOrEmpty(s))!);
		}

		foreach (var scroll in scrolls) {
			if (prices.TryGetValue(scroll.ToUpper(), out var price)) {
				var value = price * NetworthConstants.ApplicationWorth.NecronBladeScroll;
				totalValue += value;

				item.Calculation.Add(new NetworthCalculation {
					Id = scroll.ToUpper(),
					Type = "NECRON_SCROLL",
					Value = value,
					Count = 1
				});
			}
		}

		return new NetworthCalculationData { Value = totalValue };
	}
}
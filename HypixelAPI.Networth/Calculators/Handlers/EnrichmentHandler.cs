using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators.Handlers;

public class EnrichmentHandler : IItemNetworthHandler
{
	public bool Applies(NetworthItem item) {
		return item.Attributes?.Extra != null &&
		       item.Attributes.Extra.TryGetValue("talisman_enrichment", out var enrichment) &&
		       !string.IsNullOrEmpty(enrichment.ToString());
	}

	public NetworthCalculationData Calculate(NetworthItem item, Dictionary<string, double> prices) {
		if (item.Attributes?.Extra == null ||
		    !item.Attributes.Extra.TryGetValue("talisman_enrichment", out var enrichmentObj)) {
			return new NetworthCalculationData();
		}

		var enrichment = enrichmentObj.ToString();
		if (string.IsNullOrEmpty(enrichment)) return new NetworthCalculationData();

		// Calculate min price of all enrichments
		var minEnrichmentPrice = double.MaxValue;
		foreach (var enrich in NetworthConstants.Enrichments) {
			if (prices.TryGetValue(enrich, out var p)) {
				if (p < minEnrichmentPrice) minEnrichmentPrice = p;
			}
		}

		if (Math.Abs(minEnrichmentPrice - double.MaxValue) > double.Epsilon) {
			var value = minEnrichmentPrice * NetworthConstants.ApplicationWorth.Enrichment;

			item.Calculation ??= new List<NetworthCalculation>();
			item.Calculation.Add(new NetworthCalculation {
				Id = enrichment.ToUpper(),
				Type = "TALISMAN_ENRICHMENT",
				Value = value,
				Count = 1
			});

			return new NetworthCalculationData { Value = value };
		}

		return new NetworthCalculationData();
	}
}
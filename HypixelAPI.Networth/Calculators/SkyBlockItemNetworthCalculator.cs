using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Calculators;

public class SkyBlockItemNetworthCalculator
{
	private readonly List<IItemNetworthHandler> _handlers;

	public SkyBlockItemNetworthCalculator(IEnumerable<IItemNetworthHandler> handlers) {
		_handlers = handlers.ToList();
	}

	public SkyBlockItemNetworthCalculator() : this(new List<IItemNetworthHandler> {
		new RecombobulatorHandler(),
		new PotatoBooksHandler(),
		new GemstonesHandler(),
		new EssenceStarsHandler(),
		new MasterStarsHandler(),
		new ArtOfPeaceHandler(),
		new ArtOfWarHandler(),
		new AvariceCoinsCollectedHandler(),
		new BoosterHandler(),
		new DivanPowderCoatingHandler(),
		new EnrichmentHandler(),
		new EtherwarpConduitHandler(),
		new FarmingForDummiesHandler(),
		new GemstonePowerScrollHandler(),
		new JalapenoBookHandler(),
		new ManaDisintegratorHandler(),
		new MidasWeaponHandler(),
		new NecronBladeScrollsHandler(),
		new NewYearCakeHandler(),
		new PickonimbusHandler(),
		new PocketSackInASackHandler(),
		new PolarvoidBookHandler(),
		new PrestigeHandler(),
		new PulseRingThunderHandler(),
		new RodPartsHandler(),
		new ShensAuctionHandler(),
		new TransmissionTunerHandler(),
		new WoodSingularityHandler(),
		new DrillPartsHandler(),
		new DyeHandler(),
		new EnchantedBookHandler(),
		new RuneHandler(),
		new SoulboundPetSkinHandler(),
		new SoulboundSkinHandler(),

		new EnchantmentHandler(),
		new ReforgeHandler()
	}) { }

	public async Task<NetworthResult> CalculateAsync(NetworthItem item, Dictionary<string, double> prices) {
		var result = new NetworthResult { Item = item };

		// Base price logic
		// Base price logic - simple lookup by SkyblockId
		// For now, simple lookup
		if (item.SkyblockId != null && prices.TryGetValue(item.SkyblockId, out var price)) {
			result.BasePrice = price;
			result.Price = price;
		}
		else {
			result.BasePrice = 0;
			result.Price = 0;
		}

		item.BasePrice = result.BasePrice;
		item.Price = result.Price;
		item.Calculation ??= new List<NetworthCalculation>();

		// Apply handlers
		// Note: Handlers modify item.Price directly and return the value they added
		// The return value is currently not used but could be for validation/logging
		foreach (var handler in _handlers) {
			if (handler.Applies(item)) {
				var value = handler.Calculate(item, prices);
				// item.Price is updated inside the handler
			}
		}

		// Recursive inventory calculation
		if (item.Attributes?.Inventory?.Count > 0) {
			double inventoryValue = 0;
			foreach (var subItem in item.Attributes.Inventory.Values) {
				if (subItem is null) continue;

				var subResult = await CalculateAsync(subItem, prices);
				inventoryValue += subResult.Networth;
			}

			if (inventoryValue > 0) {
				item.Price += inventoryValue;
				item.Calculation.Add(new NetworthCalculation {
					Id = "INVENTORY",
					Type = "INVENTORY",
					Value = inventoryValue,
					Count = 1
				});
			}
		}

		result.Price = item.Price;
		result.BasePrice = item.BasePrice;
		result.Networth = result.Price * item.Count;
		result.Calculation = item.Calculation;

		return result;
	}
}
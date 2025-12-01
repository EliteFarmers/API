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
		new SkinHandler(),
		new EnchantmentHandler(),
		new ReforgeHandler()
	}) { }

	public async Task<NetworthResult> CalculateAsync(NetworthItem item, Dictionary<string, double> prices) {
		var result = new NetworthResult {
			Item = new NetworthItemSimple {
				SkyblockId = item.SkyblockId,
				Name = item.Name,
				Slot = item.Slot,
				Count = item.Count,
				Damage = item.Damage,
				Uuid = item.Uuid
			}
		};

		// Base price logic
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
		item.Calculation ??= [];

		double totalHandlerValue = 0;
		double cosmeticValue = 0;
		double totalHandlerIlliquidValue = 0;

		// Apply handlers
		foreach (var handler in _handlers) {
			if (handler.Applies(item)) {
				var data = handler.Calculate(item, prices);
				totalHandlerValue += data.Value;
				totalHandlerIlliquidValue += data.IlliquidValue;
				if (data.IsCosmetic) {
					cosmeticValue += data.Value;
				}
			}
		}

		item.Price += totalHandlerValue;

		// Recursive inventory calculation
		if (item.Attributes?.Inventory?.Count > 0) {
			// Console.WriteLine($"[DEBUG] Calculator: Processing inventory with {item.Attributes.Inventory.Count} items");
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

		// Calculate modes
		result.CosmeticValue = cosmeticValue;
		
		// If the item is soulbound or not tradable, the entire value is soulbound
		if (item.IsSoulbound || !item.IsTradable) {
			result.IlliquidValue = result.Networth;
		} else {
			result.IlliquidValue = totalHandlerIlliquidValue;
		}

		result.LiquidNetworth = result.Networth - result.IlliquidValue;
		result.FunctionalNetworth = result.Networth - result.CosmeticValue;
		result.LiquidFunctionalNetworth = result.Networth - result.IlliquidValue - result.CosmeticValue;

		return result;
	}
}
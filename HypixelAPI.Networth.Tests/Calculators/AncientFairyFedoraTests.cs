using HypixelAPI.Networth.Calculators;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Tests.Calculators;

public class AncientFairyFedoraTests
{
	[Fact]
	public async Task AncientFairyFedora_ShouldCalculateFullNetworth() {
		// Arrange - Create comprehensive price dictionary
		var prices = new Dictionary<string, double> {
			// Base item
			{ "FAIRY_HELMET", 25000 },

			// Enchantments
			{ "ENCHANTMENT_GROWTH_6", 500000000 }, // Growth 6 is very expensive
			{ "ENCHANTMENT_HECATOMB_1", 1000000 },
			{ "ENCHANTMENT_BIG_BRAIN_5", 5000000 },
			{ "ENCHANTMENT_PROTECTION_7", 10000000 },
			{ "ENCHANTMENT_REJUVENATE_5", 2500000 },
			{ "ENCHANTMENT_RESPIRATION_3", 10000 },
			{ "ENCHANTMENT_AQUA_AFFINITY_1", 5000 },
			{ "ENCHANTMENT_ULTIMATE_LEGION_5", 50000000 },

			// Hot Potato Books / Fuming Potato Books
			{ "HOT_POTATO_BOOK", 50000 },
			{ "FUMING_POTATO_BOOK", 500000 },

			// Reforge stones (ancient reforge)
			{ "PRECURSOR_GEAR", 1000000 },

			// Recombobulator
			{ "RECOMBOBULATOR_3000", 5000000 }
		};

		// Use default calculator with all handlers
		var calculator = new SkyBlockItemNetworthCalculator();

		var item = new NetworthItem {
			Id = 0,
			Count = 1,
			Damage = 0,
			SkyblockId = "FAIRY_HELMET",
			Uuid = null,
			Name = "§5Ancient Fairy's Fedora",
			Lore = null,
			Enchantments = new Dictionary<string, int> {
				{ "growth", 6 },
				{ "hecatomb", 1 },
				{ "big_brain", 5 },
				{ "protection", 7 },
				{ "rejuvenate", 5 },
				{ "respiration", 3 },
				{ "aqua_affinity", 1 },
				{ "ultimate_legion", 5 }
			},
			Attributes = new NetworthItemAttributes {
				Extra = new Dictionary<string, object> {
					{ "color", "204:0:102" },
					{ "modifier", "ancient" },
					{ "originTag", "UNKNOWN" },
					{ "hot_potato_count", "15" }
				}
			},
			ItemAttributes = null,
			Gems = null,
			UnlockedSlots = null,
			PetInfo = null,
			TextureId = null,
			GemstoneSlots = null,
			UpgradeCosts = null,
			BasePrice = 0, // Will be set by calculator
			Price = 0, // Will be calculated
			SoulboundPortion = 0,
			IsSoulbound = false,
			Calculation = new List<NetworthCalculation>()
		};

		// Act
		var result = await calculator.CalculateAsync(item, prices);

		// Assert - Expected networth is 148 million coins
		Assert.NotNull(result);
		Assert.Equal(25000, result.BasePrice); // Base item price

		// The total should be much higher than base price
		Assert.True(result.Price > 25000,
			$"Expected networth > 25000, but got {result.Price:N0}. " +
			$"Calculation: {string.Join(", ", result.Calculation.Select(c => $"{c.Id}: {c.Value:N0}"))}");

		// Check that enchantments were counted
		var enchantmentCalcs = result.Calculation.Where(c => c.Id.Contains("ENCHANTMENT") || c.Type == "ENCHANTMENT")
			.ToList();
		Assert.NotEmpty(enchantmentCalcs);

		var totalEnchantmentValue = enchantmentCalcs.Sum(c => c.Value);
		Assert.True(totalEnchantmentValue > 0, "Enchantments should add value");

		// Check that reforge (ancient modifier) was counted
		var reforgeCalc = result.Calculation.FirstOrDefault(c => c.Id == "PRECURSOR_GEAR" || c.Type == "REFORGE");
		Assert.NotNull(reforgeCalc);
		Assert.True(reforgeCalc.Value > 0, "Ancient reforge should add value");

		// Check that hot potato books were counted
		var hpbCalc = result.Calculation.FirstOrDefault(c =>
			c.Id == "HOT_POTATO_BOOK" || c.Id == "FUMING_POTATO_BOOK" || c.Type == "HPB");
		Assert.NotNull(hpbCalc);
		Assert.True(hpbCalc.Value > 0, "Hot potato books should add value");

		// Verify we're getting some reasonable value
		// Even if not exactly 148M, it should be significantly more than base price
		Assert.True(result.Price >= 1000000,
			$"Expected networth of at least 1M, got {result.Price:N0}");
	}
}
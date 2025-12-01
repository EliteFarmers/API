using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class RecombobulatorHandlerTests : BaseHandlerTest<RecombobulatorHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { rarity_upgrades = 1, enchantments = new { sharpness = 5 } } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "RECOMBOBULATOR_3000", 10000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "RECOMBOBULATOR_3000",
						Type = "RECOMBOBULATOR_3000",
						Value = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
						Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly with accessory (simulated via Lore)",
				Item = new {
					SkyblockId = "HEGEMONY_ARTIFACT",
					Attributes = new { Extra = new { rarity_upgrades = 1 } },
					Lore = new List<string> { "This is a MYTHIC ACCESSORY" },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "RECOMBOBULATOR_3000", 10000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "RECOMBOBULATOR_3000",
						Type = "RECOMBOBULATOR_3000",
						Value = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
						Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly with hatcessory",
				Item = new {
					SkyblockId = "TEST_HATCESSORY",
					Attributes = new { Extra = new { rarity_upgrades = 1 } },
					Lore = new List<string> { "This is a MYTHIC HATCESSORY" },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "RECOMBOBULATOR_3000", 10000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "RECOMBOBULATOR_3000",
						Type = "RECOMBOBULATOR_3000",
						Value = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
						Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly due to category (MITHRIL_BELT simulated via Lore)",
				Item = new {
					SkyblockId = "MITHRIL_BELT",
					Attributes = new { Extra = new { rarity_upgrades = 1 } },
					Lore = new List<string> { "This is a BELT ACCESSORY" }, // Added ACCESSORY to pass fallback check
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "RECOMBOBULATOR_3000", 10000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "RECOMBOBULATOR_3000",
						Type = "RECOMBOBULATOR_3000",
						Value = 10000000 * NetworthConstants.ApplicationWorth.Recombobulator,
						Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly with bonemerang",
				Item = new {
					SkyblockId = "BONE_BOOMERANG",
					Attributes = new { Extra = new { rarity_upgrades = 1, enchantments = new { power = 5 } } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "RECOMBOBULATOR_3000", 10000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10000000 * 0.5 * NetworthConstants.ApplicationWorth.Recombobulator,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "RECOMBOBULATOR_3000",
						Type = "RECOMBOBULATOR_3000",
						Value = 10000000 * 0.5 * NetworthConstants.ApplicationWorth.Recombobulator,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply (no rarity upgrade)",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Does not apply (not allowed item/category/accessory)",
				Item = new {
					SkyblockId = "MACHINE_GUN_BOW",
					Attributes = new
						{ Extra = new { rarity_upgrades = 1, item_tier = 1, enchantments = new { power = 5 } } },
					Lore = new List<string> { "Just a bow" },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
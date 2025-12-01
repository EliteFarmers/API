using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class EssenceStarsHandlerTests : BaseHandlerTest<EssenceStarsHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "HYPERION",
					UpgradeCosts = new List<List<object>> {
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 10 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 20 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 30 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 40 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 50 } }
					},
					Attributes = new { Extra = new { dungeon_item_level = "3b" } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "ESSENCE_WITHER", 100 } },
				ShouldApply = true,
				ExpectedPriceChange = (10 + 20 + 30) * 100 * NetworthConstants.ApplicationWorth.Essence,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 10 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 10
					},
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 20 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 20
					},
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 30 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 30
					}
				}
			},
			new() {
				Description = "Applies correctly when no prices",
				Item = new {
					SkyblockId = "HYPERION",
					UpgradeCosts = new List<List<object>>
						{ new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 10 } } },
					Attributes = new { Extra = new { dungeon_item_level = "1b" } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = 0,
				ExpectedCalculation = new List<NetworthCalculation>()
			},
			new() {
				Description = "Applies correctly when above range",
				Item = new {
					SkyblockId = "HYPERION",
					UpgradeCosts = new List<List<object>> {
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 10 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 20 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 30 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 40 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 50 } }
					},
					Attributes = new { Extra = new { upgrade_level = 10 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "ESSENCE_WITHER", 100 } },
				ShouldApply = true,
				ExpectedPriceChange = (10 + 20 + 30 + 40 + 50) * 100 * NetworthConstants.ApplicationWorth.Essence,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 10 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 10
					},
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 20 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 20
					},
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 30 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 30
					},
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 40 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 40
					},
					new() {
						Id = "ESSENCE_WITHER", Type = "STAR",
						Value = 50 * 100 * NetworthConstants.ApplicationWorth.Essence, Count = 50
					}
				}
			},
			new() {
				Description = "Does not apply 1 (no upgrade costs)",
				Item = new {
					SkyblockId = "HYPERION",
					Attributes = new { Extra = new { } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Does not apply 2 (no level)",
				Item = new {
					SkyblockId = "GENERALS_ARMOR_OF_THE_RESISTANCE_LEGGINGS",
					Attributes = new { Extra = new { dungeon_item_level = "1b" } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Does not apply 3 (level 0)",
				Item = new {
					SkyblockId = "HYPERION",
					UpgradeCosts = new List<List<object>> {
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 10 } }
					},
					Attributes = new { Extra = new { upgrade_level = 0 } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
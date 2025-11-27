using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class AvariceCoinsCollectedHandlerTests : BaseHandlerTest<AvariceCoinsCollectedHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "CROWN_OF_AVARICE",
					Attributes = new { Extra = new { collected_coins = 500_000_000.0 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "CROWN_OF_AVARICE", 250_000_000 }, { "CROWN_OF_AVARICE_1B", 4_500_000_000 } },
				ShouldApply = true,
				ExpectedNewBasePrice = 2_375_000_000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "CROWN_OF_AVARICE",
						Type = "CROWN_OF_AVARICE",
						Value = 2_375_000_000,
						Count = 500_000_000
					}
				}
			},
			new() {
				Description = "Applies correctly when maxed",
				Item = new {
					SkyblockId = "CROWN_OF_AVARICE",
					Attributes = new { Extra = new { collected_coins = 1_000_000_000.0 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "CROWN_OF_AVARICE", 250_000_000 }, { "CROWN_OF_AVARICE_1B", 4_500_000_000 } },
				ShouldApply = true,
				ExpectedNewBasePrice = 4_500_000_000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "CROWN_OF_AVARICE",
						Type = "CROWN_OF_AVARICE",
						Value = 4_500_000_000,
						Count = 1_000_000_000
					}
				}
			},
			new() {
				Description = "Applies correctly when over max",
				Item = new {
					SkyblockId = "CROWN_OF_AVARICE",
					Attributes = new { Extra = new { collected_coins = 10_000_000_000.0 } },
					BasePrice = 100,
					Price = 100
				},
				Prices = new Dictionary<string, double>
					{ { "CROWN_OF_AVARICE", 250_000_000 }, { "CROWN_OF_AVARICE_1B", 4_500_000_000 } },
				ShouldApply = true,
				ExpectedNewBasePrice = 4_500_000_000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "CROWN_OF_AVARICE",
						Type = "CROWN_OF_AVARICE",
						Value = 4_500_000_000,
						Count = 1_000_000_000
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "IRON_SWORD",
					Attributes = new { Extra = new { } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Does not apply with 0 coins collected",
				Item = new {
					SkyblockId = "CROWN_OF_AVARICE",
					Attributes = new { Extra = new { collected_coins = 0 } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
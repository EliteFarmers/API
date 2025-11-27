using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class MasterStarsHandlerTests : BaseHandlerTest<MasterStarsHandler>
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
					Attributes = new { Extra = new { upgrade_level = 10 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> {
					{ "FIRST_MASTER_STAR", 15000000 },
					{ "SECOND_MASTER_STAR", 25000000 },
					{ "THIRD_MASTER_STAR", 50000000 },
					{ "FOURTH_MASTER_STAR", 90000000 },
					{ "FIFTH_MASTER_STAR", 100000000 }
				},
				ShouldApply = true,
				ExpectedPriceChange =
					15000000 * NetworthConstants.ApplicationWorth.MasterStar +
					25000000 * NetworthConstants.ApplicationWorth.MasterStar +
					50000000 * NetworthConstants.ApplicationWorth.MasterStar +
					90000000 * NetworthConstants.ApplicationWorth.MasterStar +
					100000000 * NetworthConstants.ApplicationWorth.MasterStar,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "FIRST_MASTER_STAR", Type = "MASTER_STAR",
						Value = 15000000 * NetworthConstants.ApplicationWorth.MasterStar, Count = 1
					},
					new() {
						Id = "SECOND_MASTER_STAR", Type = "MASTER_STAR",
						Value = 25000000 * NetworthConstants.ApplicationWorth.MasterStar, Count = 1
					},
					new() {
						Id = "THIRD_MASTER_STAR", Type = "MASTER_STAR",
						Value = 50000000 * NetworthConstants.ApplicationWorth.MasterStar, Count = 1
					},
					new() {
						Id = "FOURTH_MASTER_STAR", Type = "MASTER_STAR",
						Value = 90000000 * NetworthConstants.ApplicationWorth.MasterStar, Count = 1
					},
					new() {
						Id = "FIFTH_MASTER_STAR", Type = "MASTER_STAR",
						Value = 100000000 * NetworthConstants.ApplicationWorth.MasterStar, Count = 1
					}
				}
			},
			new() {
				Description = "Applies correctly (dungeon_item_level)",
				Item = new {
					SkyblockId = "HYPERION",
					UpgradeCosts = new List<List<object>> {
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 10 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 20 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 30 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 40 } },
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 50 } }
					},
					Attributes = new { Extra = new { dungeon_item_level = "6b" } },
					Price = 100
				},
				Prices = new Dictionary<string, double> {
					{ "FIRST_MASTER_STAR", 15000000 },
					{ "SECOND_MASTER_STAR", 25000000 },
					{ "THIRD_MASTER_STAR", 50000000 },
					{ "FOURTH_MASTER_STAR", 90000000 },
					{ "FIFTH_MASTER_STAR", 100000000 }
				},
				ShouldApply = true,
				ExpectedPriceChange = 15000000 * NetworthConstants.ApplicationWorth.MasterStar,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "FIRST_MASTER_STAR", Type = "MASTER_STAR",
						Value = 15000000 * NetworthConstants.ApplicationWorth.MasterStar, Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "HYPERION",
					UpgradeCosts = new List<List<object>> {
						new List<object> { new { Type = "ESSENCE", ItemId = "WITHER", Amount = 10 } }
					},
					Attributes = new { Extra = new { upgrade_level = 5 } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PetItemHandlerTests : BaseHandlerTest<PetItemHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					PetInfo = new { HeldItem = "PET_ITEM_MINING_SKILL_BOOST_UNCOMMON", Type = "CAT", Tier = "COMMON" },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "PET_ITEM_MINING_SKILL_BOOST_UNCOMMON", 200000 } },
				ShouldApply = true,
				ExpectedPriceChange = 200000 * NetworthConstants.ApplicationWorth.PetItem,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "PET_ITEM_MINING_SKILL_BOOST_UNCOMMON",
						Type = "PET_ITEM",
						Value = 200000 * NetworthConstants.ApplicationWorth.PetItem,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					PetInfo = new { Type = "CAT", Tier = "COMMON" }, // No HeldItem
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
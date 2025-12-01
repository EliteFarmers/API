using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class FarmingForDummiesHandlerTests : BaseHandlerTest<FarmingForDummiesHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "THEORETICAL_HOE_CARROT_3",
					Attributes = new { Extra = new { farming_for_dummies_count = 5 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "FARMING_FOR_DUMMIES", 2000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 5 * 2000000 * NetworthConstants.ApplicationWorth.FarmingForDummies,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "FARMING_FOR_DUMMIES",
						Type = "FARMING_FOR_DUMMIES",
						Value = 5 * 2000000 * NetworthConstants.ApplicationWorth.FarmingForDummies,
						Count = 5
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "THEORETICAL_HOE_CARROT_3",
					Attributes = new { Extra = new { } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
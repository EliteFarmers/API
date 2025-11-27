using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class WoodSingularityHandlerTests : BaseHandlerTest<WoodSingularityHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "TACTICIAN_SWORD",
					Attributes = new { Extra = new { wood_singularity_count = 1 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "WOOD_SINGULARITY", 7000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 7000000 * NetworthConstants.ApplicationWorth.WoodSingularity,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "WOOD_SINGULARITY",
						Type = "WOOD_SINGULARITY",
						Value = 7000000 * NetworthConstants.ApplicationWorth.WoodSingularity,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "TACTICIAN_SWORD",
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
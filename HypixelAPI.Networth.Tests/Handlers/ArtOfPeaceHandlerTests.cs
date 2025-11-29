using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class ArtOfPeaceHandlerTests : BaseHandlerTest<ArtOfPeaceHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "LEATHER_CHESTPLATE",
					Attributes = new { Extra = new { artOfPeaceApplied = 3 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "THE_ART_OF_PEACE", 50000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 3 * 50000000 * NetworthConstants.ApplicationWorth.ArtOfPeace,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "THE_ART_OF_PEACE",
						Type = "THE_ART_OF_PEACE",
						Value = 3 * 50000000 * NetworthConstants.ApplicationWorth.ArtOfPeace,
						Count = 3
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "LEATHER_CHESTPLATE",
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
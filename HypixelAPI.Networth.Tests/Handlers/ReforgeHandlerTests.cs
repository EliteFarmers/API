using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class ReforgeHandlerTests : BaseHandlerTest<ReforgeHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "SUPERIOR_DRAGON_HELMET",
					Attributes = new { Extra = new { modifier = "renowned" } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "DRAGON_HORN", 10000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 10000000 * NetworthConstants.ApplicationWorth.Reforge,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "DRAGON_HORN",
						Type = "REFORGE",
						Value = 10000000 * NetworthConstants.ApplicationWorth.Reforge,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "IRON_HELMET",
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
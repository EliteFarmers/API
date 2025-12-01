using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class RuneHandlerTests : BaseHandlerTest<RuneHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "SUPERIOR_DRAGON_HELMET",
					Attributes = new { Extra = new { runes = new { GRAND_SEARING = 3 } } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "RUNE_GRAND_SEARING_3", 1200000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 1200000000 * NetworthConstants.ApplicationWorth.Runes,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "RUNE_GRAND_SEARING_3",
						Type = "RUNE",
						Value = 1200000000 * NetworthConstants.ApplicationWorth.Runes,
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
			},
			new() {
				Description = "Does not apply with rune item",
				Item = new {
					SkyblockId = "RUNE",
					Attributes = new { Extra = new { runes = new { GRAND_SEARING = 3 } } },
					Price = 100
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
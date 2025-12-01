using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class RodPartsHandlerTests : BaseHandlerTest<RodPartsHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "ROD_OF_THE_SEA",
					Attributes = new {
						Extra = new {
							line = new { part = "titan_line" },
							hook = new { part = "hotspot_hook" },
							sinker = new { part = "hotspot_sinker" }
						}
					},
					Price = 100
				},
				Prices = new Dictionary<string, double> {
					{ "TITAN_LINE", 220000000 },
					{ "HOTSPOT_HOOK", 16000000 },
					{ "HOTSPOT_SINKER", 16000000 }
				},
				ShouldApply = true,
				ExpectedPriceChange = 220000000 * NetworthConstants.ApplicationWorth.RodPart +
				                      16000000 * NetworthConstants.ApplicationWorth.RodPart +
				                      16000000 * NetworthConstants.ApplicationWorth.RodPart,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "TITAN_LINE", Type = "ROD_PART",
						Value = 220000000 * NetworthConstants.ApplicationWorth.RodPart, Count = 1
					},
					new() {
						Id = "HOTSPOT_HOOK", Type = "ROD_PART",
						Value = 16000000 * NetworthConstants.ApplicationWorth.RodPart, Count = 1
					},
					new() {
						Id = "HOTSPOT_SINKER", Type = "ROD_PART",
						Value = 16000000 * NetworthConstants.ApplicationWorth.RodPart, Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "ROD_OF_THE_SEA",
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
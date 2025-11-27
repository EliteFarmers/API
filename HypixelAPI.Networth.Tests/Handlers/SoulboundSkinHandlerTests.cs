using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class SoulboundSkinHandlerTests : BaseHandlerTest<SoulboundSkinHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "DIAMOND_NECRON_HEAD",
					Attributes = new { Extra = new { skin = "NECRON_DIAMOND_KNIGHT" } },
					IsSoulbound = true // Simulated from Lore
				},
				Prices = new Dictionary<string, double> { { "NECRON_DIAMOND_KNIGHT", 60000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 60000000 * NetworthConstants.ApplicationWorth.SoulboundSkins,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "NECRON_DIAMOND_KNIGHT",
						Type = "SOULBOUND_SKIN",
						Value = 60000000 * NetworthConstants.ApplicationWorth.SoulboundSkins,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply with no skin",
				Item = new {
					SkyblockId = "DIAMOND_NECRON_HEAD",
					Attributes = new { Extra = new { } },
					IsSoulbound = true
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Does not apply when not soulbound",
				Item = new {
					SkyblockId = "DIAMOND_NECRON_HEAD",
					Attributes = new { Extra = new { skin = "NECRON_DIAMOND_KNIGHT" } },
					IsSoulbound = false
				},
				Prices = new Dictionary<string, double> { { "NECRON_DIAMOND_KNIGHT", 60000000 } },
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
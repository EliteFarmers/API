using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class SoulboundPetSkinHandlerTests : BaseHandlerTest<SoulboundPetSkinHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					PetInfo = new {
						Type = "GRANDMA_WOLF",
						Tier = "LEGENDARY",
						Exp = 0,
						Skin = "GRANDMA_WOLF_REAL"
					},
					IsSoulbound = true // Explicitly set as handler checks this
				},
				Prices = new Dictionary<string, double> { { "PET_SKIN_GRANDMA_WOLF_REAL", 65000000 } },
				ShouldApply = true,
				ExpectedPriceChange = 65000000 * NetworthConstants.ApplicationWorth.SoulboundPetSkins,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "GRANDMA_WOLF_REAL",
						Type = "SOULBOUND_PET_SKIN",
						Value = 65000000 * NetworthConstants.ApplicationWorth.SoulboundPetSkins,
						Count = 1
					}
				}
			},
			new() {
				Description = "Does not apply (no skin)",
				Item = new {
					PetInfo = new {
						Type = "BLACK_CAT",
						Tier = "MYTHIC",
						Exp = 0
					},
					IsSoulbound = true
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Does not apply (not soulbound)",
				Item = new {
					PetInfo = new {
						Type = "GRANDMA_WOLF",
						Tier = "LEGENDARY",
						Exp = 0,
						Skin = "GRANDMA_WOLF_REAL"
					},
					IsSoulbound = false
				},
				Prices = new Dictionary<string, double> { { "PET_SKIN_GRANDMA_WOLF_REAL", 65000000 } },
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
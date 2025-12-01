using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PetCandyHandlerTests : BaseHandlerTest<PetCandyHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					PetInfo = new { CandyUsed = 10, Type = "BEE", Tier = "LEGENDARY" },
					BasePrice = 100000,
					Price = 100000
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = 100000 * NetworthConstants.ApplicationWorth.PetCandy - 100000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "CANDY",
						Type = "PET_CANDY",
						Value = -(100000 -
						          (100000 * NetworthConstants.ApplicationWorth
							          .PetCandy)), // Value is negative reduction
						Count = 10
					}
				}
			},
			new() {
				Description = "Applies correctly with cap (simulated max reduction)",
				Item = new {
					PetInfo = new { CandyUsed = 10, Type = "BEE", Tier = "LEGENDARY" },
					BasePrice = 100000000,
					Price = 100000000
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = -5000000, // C# handler defaults to 5m cap
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "CANDY",
						Type = "PET_CANDY",
						Value = -5000000,
						Count = 10
					}
				}
			},
			new() {
				Description = "Does not apply (no candy)",
				Item = new {
					PetInfo = new { CandyUsed = 0, Type = "BEE", Tier = "LEGENDARY" },
					BasePrice = 50000,
					Price = 50000
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			}
		};

		RunTests(testCases);
	}
}
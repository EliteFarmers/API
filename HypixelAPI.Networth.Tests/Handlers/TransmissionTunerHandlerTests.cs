using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Constants;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class TransmissionTunerHandlerTests : BaseHandlerTest<TransmissionTunerHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "ASPECT_OF_THE_END",
					Attributes = new { Extra = new { tuned_transmission = 4 } },
					Price = 100
				},
				Prices = new Dictionary<string, double> { { "TRANSMISSION_TUNER", 50000 } },
				ShouldApply = true,
				ExpectedPriceChange = 4 * 50000 * NetworthConstants.ApplicationWorth.TunedTransmission,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "TRANSMISSION_TUNER",
						Type = "TRANSMISSION_TUNER",
						Value = 4 * 50000 * NetworthConstants.ApplicationWorth.TunedTransmission,
						Count = 4
					}
				}
			},
			new() {
				Description = "Does not apply",
				Item = new {
					SkyblockId = "ASPECT_OF_THE_END",
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
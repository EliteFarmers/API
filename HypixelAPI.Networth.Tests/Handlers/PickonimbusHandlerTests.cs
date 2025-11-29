using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Models;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PickonimbusHandlerTests : BaseHandlerTest<PickonimbusHandler>
{
	[Fact]
	public void RunTestCases() {
		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Applies correctly",
				Item = new {
					SkyblockId = "PICKONIMBUS",
					Attributes = new { Extra = new { pickonimbus_durability = 2500 } },
					BasePrice = 50000,
					Price = 50000
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = -25000,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "PICKONIMBUS_DURABLITY",
						Type = "PICKONIMBUS",
						Value = -25000,
						Count = 2500
					}
				}
			},
			new() {
				Description = "Does not apply (no extra)",
				Item = new {
					SkyblockId = "PICKONIMBUS",
					Attributes = new { Extra = new { } },
					BasePrice = 50000,
					Price = 50000
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = false
			},
			new() {
				Description = "Applies (durability 5000)",
				Item = new {
					SkyblockId = "PICKONIMBUS",
					Attributes = new { Extra = new { pickonimbus_durability = 5000 } },
					BasePrice = 50000,
					Price = 50000
				},
				Prices = new Dictionary<string, double>(),
				ShouldApply = true,
				ExpectedPriceChange = 0,
				ExpectedCalculation = new List<NetworthCalculation> {
					new() {
						Id = "PICKONIMBUS_DURABLITY",
						Type = "PICKONIMBUS",
						Value = 0,
						Count = 5000
					}
				}
			}
		};

		RunTests(testCases);
	}
}
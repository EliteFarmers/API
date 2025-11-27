using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Tests.Infrastructure;

namespace HypixelAPI.Networth.Tests.Handlers;

public class PrestigeHandlerTests : BaseHandlerTest<PrestigeHandler>
{
	[Fact]
	public void RunTestCases() {
		// NOTE: PrestigeHandler isn't complete yet

		var testCases = new List<HandlerTestCase> {
			new() {
				Description = "Does not apply (no prestige)",
				Item = new {
					SkyblockId = "IRON_SWORD",
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
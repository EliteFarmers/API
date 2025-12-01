using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Tests.Infrastructure;

public class HandlerTestCase
{
	public required string Description { get; set; }
	public required object Item { get; set; }
	public Dictionary<string, double> Prices { get; set; } = new();
	public bool ShouldApply { get; set; }
	public double? ExpectedPriceChange { get; set; }
	public double? ExpectedNewBasePrice { get; set; }
	public List<NetworthCalculation> ExpectedCalculation { get; set; } = new();
}

public abstract class BaseHandlerTest<THandler> where THandler : IItemNetworthHandler, new()
{
	protected readonly THandler Handler = new();

	protected void RunTests(List<HandlerTestCase> testCases) {
		foreach (var testCase in testCases) {
			RunTest(testCase);
		}
	}

	protected void RunTest(HandlerTestCase testCase) {
		// Arrange
		var item = TestHelpers.CreateItem(testCase.Item);
		var prices = testCase.Prices;

		// Act
		var applies = Handler.Applies(item);

		// Assert - Applies
		if (applies != testCase.ShouldApply) {
			Console.WriteLine($"[FAIL] Applies mismatch. Test: '{testCase.Description}'. Expected: {testCase.ShouldApply}, Actual: {applies}");
		}
		Assert.True(applies == testCase.ShouldApply,
			$"Test '{testCase.Description}': Expected Applies to be {testCase.ShouldApply}, but was {applies}");

		if (applies) {
			var result = Handler.Calculate(item, prices);

			// Assert - Price Change
			if (testCase.ExpectedPriceChange.HasValue) {
				if (Math.Abs(result.Value - testCase.ExpectedPriceChange.Value) >= 0.001) {
					Console.WriteLine($"[FAIL] PriceChange mismatch. Test: '{testCase.Description}'. Expected: {testCase.ExpectedPriceChange}, Actual: {result.Value}");
				}
				Assert.True(Math.Abs(result.Value - testCase.ExpectedPriceChange.Value) < 0.001,
					$"Test '{testCase.Description}': Expected result {testCase.ExpectedPriceChange}, but got {result.Value}");
			}

			// Assert - New Base Price
			if (testCase.ExpectedNewBasePrice.HasValue) {
				if (Math.Abs(item.BasePrice - testCase.ExpectedNewBasePrice.Value) >= 0.001) {
					Console.WriteLine($"[FAIL] BasePrice mismatch. Test: '{testCase.Description}'. Expected: {testCase.ExpectedNewBasePrice}, Actual: {item.BasePrice}");
				}
				Assert.True(Math.Abs(item.BasePrice - testCase.ExpectedNewBasePrice.Value) < 0.001,
					$"Test '{testCase.Description}': Expected BasePrice {testCase.ExpectedNewBasePrice}, but got {item.BasePrice}");
			}

			// Assert - Calculation
			if (testCase.ExpectedCalculation != null && testCase.ExpectedCalculation.Count > 0) {
				Assert.NotNull(item.Calculation);
				Assert.Equal(testCase.ExpectedCalculation.Count, item.Calculation.Count);

				var expectedSorted = testCase.ExpectedCalculation.OrderBy(c => c.Id).ThenBy(c => c.Type)
					.ThenBy(c => c.Value).ToList();
				var actualSorted = item.Calculation.OrderBy(c => c.Id).ThenBy(c => c.Type).ThenBy(c => c.Value)
					.ToList();

				for (int i = 0; i < expectedSorted.Count; i++) {
					var expected = expectedSorted[i];
					var actual = actualSorted[i];

					Assert.Equal(expected.Id, actual.Id);
					Assert.Equal(expected.Type, actual.Type);
					Assert.Equal(expected.Count, actual.Count);
					if (Math.Abs(expected.Value - actual.Value) >= 0.001) {
						Console.WriteLine($"[FAIL] Calculation Value mismatch. Test: '{testCase.Description}'. Calc[{i}]. Expected: {expected.Value}, Actual: {actual.Value}");
					}
					Assert.True(Math.Abs(expected.Value - actual.Value) < 0.001,
						$"Test '{testCase.Description}': Calc[{i}] Value mismatch. Expected {expected.Value}, got {actual.Value}");
				}
			}
		}
	}
}
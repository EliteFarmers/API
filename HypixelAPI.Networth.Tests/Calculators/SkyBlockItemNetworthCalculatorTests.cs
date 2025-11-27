using HypixelAPI.Networth.Calculators;
using HypixelAPI.Networth.Calculators.Handlers;
using HypixelAPI.Networth.Interfaces;
using HypixelAPI.Networth.Models;

namespace HypixelAPI.Networth.Tests.Calculators;

public class SkyBlockItemNetworthCalculatorTests
{
	private class MockHandler : IItemNetworthHandler
	{
		public bool Applies(NetworthItem item) => true;

		public double Calculate(NetworthItem item, Dictionary<string, double> prices) {
			item.Price += 100;
			return 100;
		}
	}

	private class MockPriceProvider : IPriceProvider
	{
		public Dictionary<string, double> Prices { get; set; } = new();

		public Task<Dictionary<string, double>> GetPricesAsync() {
			return Task.FromResult(Prices);
		}
	}

	[Fact]
	public async Task GetNetworth_ShouldCalculateCorrectly() {
		// Arrange
		var prices = new Dictionary<string, double> { { "DIAMOND_SWORD", 1000 } };
		var handlers = new List<IItemNetworthHandler> { new MockHandler() };
		var calculator = new SkyBlockItemNetworthCalculator(handlers);

		var item = new NetworthItem {
			SkyblockId = "DIAMOND_SWORD",
			Name = "Diamond Sword",
			Count = 1,
			Attributes = new NetworthItemAttributes
				{ Extra = new Dictionary<string, object> { { "id", "DIAMOND_SWORD" } } }
		};

		// Act
		var result = await calculator.CalculateAsync(item, prices);

		// Assert
		Assert.NotNull(result);
		Assert.Equal("DIAMOND_SWORD", result.Item.SkyblockId);
		Assert.Equal(1000 + 100, result.Price); // Base price (1000) + Handler (100)
		Assert.Equal(1000, result.BasePrice);
	}

	[Fact]
	public async Task GetNetworth_ShouldUseProvidedPrices() {
		// Arrange
		var prices = new Dictionary<string, double> { { "DIAMOND_SWORD", 2000 } }; // Different price
		var handlers = new List<IItemNetworthHandler>();
		var calculator = new SkyBlockItemNetworthCalculator(handlers);

		var item = new NetworthItem {
			SkyblockId = "DIAMOND_SWORD",
			Count = 1
		};

		// Act
		var result = await calculator.CalculateAsync(item, prices);

		// Assert
		Assert.Equal(2000, result.Price);
	}

	[Fact]
	public async Task GetNetworth_ShouldCalculateRecursiveInventory_NewYearCakeBag() {
		// Arrange
		var prices = new Dictionary<string, double> {
			{ "NEW_YEAR_CAKE_BAG", 5000 },
			{ "NEW_YEAR_CAKE_1", 1000000 },
			{ "NEW_YEAR_CAKE_2", 2000000 }
		};

		// We need the NewYearCakeHandler for the sub-items
		var handlers = new List<IItemNetworthHandler> { new NewYearCakeHandler() };
		var calculator = new SkyBlockItemNetworthCalculator(handlers);

		var cake1 = new NetworthItem {
			SkyblockId = "NEW_YEAR_CAKE",
			Attributes = new NetworthItemAttributes
				{ Extra = new Dictionary<string, object> { { "new_years_cake", 1 } } },
			Count = 1
		};

		var cake2 = new NetworthItem {
			SkyblockId = "NEW_YEAR_CAKE",
			Attributes = new NetworthItemAttributes
				{ Extra = new Dictionary<string, object> { { "new_years_cake", 2 } } },
			Count = 1
		};

		var bag = new NetworthItem {
			SkyblockId = "NEW_YEAR_CAKE_BAG",
			Count = 1,
			Attributes = new NetworthItemAttributes {
				Inventory = new Dictionary<string, NetworthItem?> {
					{ "0", cake1 },
					{ "1", cake2 }
				}
			}
		};

		// Act
		var result = await calculator.CalculateAsync(bag, prices);

		// Assert
		// Bag base price (5000) + Cake 1 (1m) + Cake 2 (2m) = 3,005,000
		Assert.Equal(3005000, result.Price);
		Assert.Contains(result.Calculation, c => c.Id == "INVENTORY" && c.Value == 3000000);
	}
}
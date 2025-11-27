using System.Text.Json;
using HypixelAPI.Networth.Interfaces;

namespace HypixelAPI.Networth.Services;

public class SkyHelperPriceProvider : IPriceProvider
{
	private static readonly HttpClient _httpClient = new();
	private static readonly SemaphoreSlim _cacheLock = new(1, 1);
	private static Dictionary<string, double>? _cachedPrices;
	private static DateTime _cacheExpiry = DateTime.MinValue;
	private static readonly TimeSpan _cacheLifetime = TimeSpan.FromMinutes(5);

	private const string PriceUrl = "https://raw.githubusercontent.com/SkyHelperBot/Prices/main/pricesV2.json";

	public async Task<Dictionary<string, double>> GetPricesAsync() {
		// Check if cache is still valid
		if (_cachedPrices != null && DateTime.UtcNow < _cacheExpiry) {
			return _cachedPrices;
		}

		await _cacheLock.WaitAsync();
		try {
			// Double-check after acquiring lock
			if (_cachedPrices != null && DateTime.UtcNow < _cacheExpiry) {
				return _cachedPrices;
			}

			// Fetch new prices
			var response = await _httpClient.GetAsync(PriceUrl);
			response.EnsureSuccessStatusCode();

			var content = await response.Content.ReadAsStringAsync();
			var prices = JsonSerializer.Deserialize<Dictionary<string, double>>(content, new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			});

			if (prices == null) {
				throw new InvalidOperationException("Failed to deserialize prices from SkyHelper.");
			}

			_cachedPrices = prices;
			_cacheExpiry = DateTime.UtcNow.Add(_cacheLifetime);

			return _cachedPrices;
		}
		finally {
			_cacheLock.Release();
		}
	}
}
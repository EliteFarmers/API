using System.Text.Json;
using System.Text.Json.Serialization;
using EliteFarmers.HypixelAPI.Handlers;
using EliteFarmers.HypixelAPI.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace EliteFarmers.HypixelAPI;

public static class DependencyInjection {
	/// <summary>
	/// Adds the Hypixel API client to the service collection with the specified options. You must provide an API key in the options.
	/// </summary>
	/// <param name="services">The service collection to add the client to.</param>
	/// <param name="options">The options for configuring the Hypixel API client.</param>
	/// <returns>An <see cref="IHttpClientBuilder"/> for further configuration.</returns>
	public static IHttpClientBuilder AddHypixelApi(this IServiceCollection services, HypixelApiOptions options) {
		services.AddSingleton<IHypixelKeyUsageCounter, HypixelKeyUsageCounter>();
		services.AddSingleton<IHypixelRequestLimiter, HypixelRequestLimiter>();
		services.AddScoped<HypixelRateLimitHandler>();

		return services.AddRefitClient<IHypixelApi>(new RefitSettings {
				ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions {
					PropertyNameCaseInsensitive = true,
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				})
			})
			.ConfigureHttpClient(opt => {
				opt.BaseAddress = new Uri(IHypixelApi.BaseHypixelUrl);
				opt.DefaultRequestHeaders.TryAddWithoutValidation("API-Key", options.ApiKey);
				if (!string.IsNullOrWhiteSpace(options.UserAgent))
					opt.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
				opt.Timeout = TimeSpan.FromSeconds(15);
			})
			.AddHttpMessageHandler<HypixelRateLimitHandler>();
	}

	/// <summary>
	/// Adds the Hypixel API client to the service collection with options configured via the provided action. You must provide an API key in the options.
	/// </summary>
	/// <param name="services">The service collection to add the client to.</param>
	/// <param name="configureOptions">An action to configure the Hypixel API client options.</param>
	/// <returns></returns>
	public static IHttpClientBuilder AddHypixelApi(this IServiceCollection services,
		Action<HypixelApiOptions> configureOptions) {
		var options = new HypixelApiOptions();
		configureOptions(options);
		return services.AddHypixelApi(options);
	}
}
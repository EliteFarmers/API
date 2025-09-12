using System.Text.Json;
using System.Text.Json.Serialization;
using HypixelAPI.Handlers;
using HypixelAPI.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace HypixelAPI;

public static class DependencyInjection 
{
	public static IHttpClientBuilder AddHypixelApi(this IServiceCollection services, string hypixelApiKey, string? userAgent = null) {
		services.AddSingleton<IHypixelKeyUsageCounter, HypixelKeyUsageCounter>();
		services.AddSingleton<IHypixelRequestLimiter, HypixelRequestLimiter>();
		services.AddScoped<HypixelRateLimitHandler>();
		
		return services.AddRefitClient<IHypixelApi>(new RefitSettings()
			{
				ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions()
				{
					PropertyNameCaseInsensitive = true,
					NumberHandling = JsonNumberHandling.AllowReadingFromString,
					DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
				})
			})
			.ConfigureHttpClient(opt => {
				opt.BaseAddress = new Uri(IHypixelApi.BaseHypixelUrl);
				opt.DefaultRequestHeaders.TryAddWithoutValidation("API-Key", hypixelApiKey);
				if (!string.IsNullOrWhiteSpace(userAgent)) {
					opt.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
				}
				opt.Timeout = TimeSpan.FromSeconds(15);
			})
			.AddHttpMessageHandler<HypixelRateLimitHandler>();
	}
}
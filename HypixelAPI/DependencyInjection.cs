using HypixelAPI.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace HypixelAPI;

public static class DependencyInjection 
{
	public static IServiceCollection AddHypixelApi(this IServiceCollection services, string hypixelApiKey, string? userAgent = null) {
		services.AddSingleton<IHypixelRequestLimiter, HypixelRequestLimiter>();
		services.AddScoped<RateLimitHandler>();
		
		services.AddRefitClient<IHypixelApi>()
			.ConfigureHttpClient(opt => {
				opt.BaseAddress = new Uri(IHypixelApi.BaseHypixelUrl);
				opt.DefaultRequestHeaders.TryAddWithoutValidation("API-Key", hypixelApiKey);
				if (!string.IsNullOrWhiteSpace(userAgent)) {
					opt.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", userAgent);
				}
			})
			.AddHttpMessageHandler<RateLimitHandler>();

		return services;
	}
}
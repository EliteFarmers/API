using EliteAPI.Background.Discord;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.AspNetCore.OutputCaching;
using Quartz;
using StackExchange.Redis;

namespace EliteAPI.Features.Shop.Products.Admin.RefreshProducts;

internal sealed class RefreshProductsEndpoint(
	IConnectionMultiplexer redis,
	IOutputCacheStore cacheStore,
	ISchedulerFactory schedulerFactory
) : EndpointWithoutRequest<List<ProductDto>> {
	
	public override void Configure() {
		Post("/products/refresh");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Refresh Shop Products";
		});
		
		Description(d => d.AutoTagOverride("Product"));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:products");
		
		var scheduler = await schedulerFactory.GetScheduler(c);
		await scheduler.TriggerJob(RefreshProductsBackgroundJob.Key, c);
		
		await cacheStore.EvictByTagAsync("products", c);

		await SendNoContentAsync(c);
	}
}
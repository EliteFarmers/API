using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Shop.Products.Admin.RemoveCosmetic;

internal sealed class RemoveCosmeticToProductRequest {
	/// <summary>
	/// Id of the produc to add the cosmetic to
	/// </summary>
	public required long ProductId { get; set; }

	/// <summary>
	/// Id of the cosmetic to add to the product
	/// </summary>
	public required int CosmeticId { get; set; }
}

internal sealed class RemoveCosmeticToProductEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IConnectionMultiplexer redis
) : Endpoint<RemoveCosmeticToProductRequest> {
	public override void Configure() {
		Delete("/product/{ProductId}/cosmetics/{CosmeticId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => { s.Summary = "Remove Cosmetic from Product"; });
	}

	public override async Task HandleAsync(RemoveCosmeticToProductRequest request, CancellationToken c) {
		var link = await context.ProductWeightStyles
			.FirstOrDefaultAsync(p =>
				p.ProductId == (ulong)request.ProductId
				&& p.WeightStyleId == request.CosmeticId, c);

		if (link is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		context.ProductWeightStyles.Remove(link);
		await context.SaveChangesAsync(c);

		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{request.CosmeticId}");

		await cacheStore.EvictByTagAsync("products", c);

		await Send.NoContentAsync(c);
	}
}
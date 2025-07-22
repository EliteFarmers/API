using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace EliteAPI.Features.Shop.Products.Admin.AddCosmetic;

internal sealed class AddCosmeticToProductRequest {
	/// <summary>
	/// Id of the produc to add the cosmetic to
	/// </summary>
	public required long ProductId { get; set; }
	
	/// <summary>
	/// Id of the cosmetic to add to the product
	/// </summary>
	public required int CosmeticId { get; set; }
}

internal sealed class AddCosmeticToProductEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IConnectionMultiplexer redis
) : Endpoint<AddCosmeticToProductRequest> {
	
	public override void Configure() {
		Post("/product/{ProductId}/cosmetics/{CosmeticId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(s => s.Accepts<AddCosmeticToProductRequest>());

		Summary(s => {
			s.Summary = "Add Cosmetic to Product";
		});
	}

	public override async Task HandleAsync(AddCosmeticToProductRequest request, CancellationToken c) {
		var product = await context.Products.FirstOrDefaultAsync(p => p.Id == (ulong) request.ProductId, cancellationToken: c);
		if (product is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var style = await context.WeightStyles.FirstOrDefaultAsync(w => w.Id == request.CosmeticId, cancellationToken: c);
		if (style is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		if (product.ProductWeightStyles.Exists(w => w.WeightStyleId == request.CosmeticId)) {
			await SendNoContentAsync(cancellation: c);
			return;
		}

		var newLink = new ProductWeightStyle {
			WeightStyleId = request.CosmeticId,
			ProductId = (ulong) request.ProductId,
		};
		
		context.ProductWeightStyles.Add(newLink);
		await context.SaveChangesAsync(c);
		
		// Clear the style list cache
		var db = redis.GetDatabase();
		await db.KeyDeleteAsync("bot:stylelist");
		await db.KeyDeleteAsync($"bot:stylelist:{request.CosmeticId}");

		await cacheStore.EvictByTagAsync("products", c);

		await SendNoContentAsync(cancellation: c);
	}
}
using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Monetization.Services;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Products.Admin.UpdateProduct;

internal sealed class UpdateProductEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore,
	IMonetizationService monetizationService
) : Endpoint<UpdateProductRequest> {
	
	public override void Configure() {
		Patch("/product/{DiscordId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Update Shop Product";
		});
	}

	public override async Task HandleAsync(UpdateProductRequest request, CancellationToken c) {
		var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.DiscordIdUlong, c);
		if (product is null) {
			await Send.NotFoundAsync(cancellation: c);
			return;
		}
		
		await monetizationService.UpdateProductAsync(request.DiscordIdUlong, request.ProductData);
		
		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("products", c);

		await Send.NoContentAsync(cancellation: c);
	}
}
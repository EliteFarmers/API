using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Services.Interfaces;
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
		Patch("/product/{ProductId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Update Shop Product";
		});
	}

	public override async Task HandleAsync(UpdateProductRequest request, CancellationToken c) {
		var product = await context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.DiscordIdUlong, c);
		if (product is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}
		
		await monetizationService.UpdateProductAsync(request.DiscordIdUlong, request.ProductData);
		
		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("products", c);

		await SendOkAsync(cancellation: c);
	}
}
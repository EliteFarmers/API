using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.RemoveProduct;

internal sealed class RemoveProductFromCategoryRequest {
	/// <summary>
	/// Id of the category to add the product to
	/// </summary>
	public required int CategoryId { get; set; }
	
	/// <summary>
	/// Id of the product to add to the category
	/// </summary>
	public required long ProductId { get; set; }
}

internal sealed class RemoveProductToCategoryEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<RemoveProductFromCategoryRequest> {
	
	public override void Configure() {
		Delete("/shop/category/{CategoryId}/product/{ProductId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Description(s => s.Accepts<RemoveProductFromCategoryRequest>());

		Summary(s => {
			s.Summary = "Remove Product from Shop Category";
		});
	}

	public override async Task HandleAsync(RemoveProductFromCategoryRequest request, CancellationToken c) {
		var existing = await context.ProductCategories
			.FirstOrDefaultAsync(e => e.CategoryId == request.CategoryId && e.ProductId == (ulong) request.ProductId, cancellationToken: c);
		
		if (existing is null) {
			await SendNotFoundAsync(c);
			return;
		}

		context.ProductCategories.Remove(existing);
		await context.SaveChangesAsync(c);
		
		await cacheStore.EvictByTagAsync("categories", c);

		await SendNoContentAsync(cancellation: c);
	}
}
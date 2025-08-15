using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Incoming;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.ReorderCategoryProducts;

internal sealed class ReorderCategoryProductsEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<ReorderCategoryProductsRequest> {
	
	public override void Configure() {
		Post("/shop/category/{CategoryId}/reorder");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Summary(s => {
			s.Summary = "Reorder Products in Shop Category";
		});
	}

	public override async Task HandleAsync(ReorderCategoryProductsRequest request, CancellationToken c) {
		var products = await context.ProductCategories
			.Where(e => e.CategoryId == request.CategoryId)
			.Select(e => new { e.ProductId, e.Order })
			.ToListAsync(cancellationToken: c);
		
		var ordering = request.Elements.Select(e => new ReorderElement<ulong> {
			Id = ulong.TryParse(e.Id, out var result) ? result : 0,
			Order = e.Order
		}).ToList();
		
		if (products.Count != ordering.Count) {
			AddError("Invalid number of products.");
		}
		
		if (ordering.Select(e => e.Id).Except(products.Select(e => e.ProductId)).Any()) {
			AddError("Invalid product id.");
		}
		
		ThrowIfAnyErrors();
		
		var ordered = ordering.OrderBy(o => o.Order).ToList();
		try {
			for (var i = 0; i < ordered.Count; i++) {
				var order = ordered[i];
				var product = products.FirstOrDefault(e => e.ProductId == order.Id);
			
				if (product is null) {
					ThrowError("Invalid product id.");
				}
			
				if (product.Order == i) {
					continue;
				}
			
				await context.ProductCategories
					.Where(e => e.CategoryId == request.CategoryId && e.ProductId == product.ProductId)
					.ExecuteUpdateAsync(e => e.SetProperty(x => x.Order, i), cancellationToken: c);
			}
		} catch {
			ThrowError("Failed to reorder products in category.");
		}
		
		await cacheStore.EvictByTagAsync("categories", c);

		await Send.NoContentAsync(cancellation: c);
	}
}
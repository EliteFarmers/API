using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.AddProduct;

internal sealed class AddProductToCategoryRequest
{
	/// <summary>
	/// Id of the category to add the product to
	/// </summary>
	public required int CategoryId { get; set; }

	/// <summary>
	/// Id of the product to add to the category
	/// </summary>
	public required long ProductId { get; set; }
}

internal sealed class AddProductToCategoryEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<AddProductToCategoryRequest>
{
	public override void Configure() {
		Post("/shop/category/{CategoryId}/product/{ProductId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(s => s.Accepts<AddProductToCategoryRequest>());

		Summary(s => { s.Summary = "Add Product to Shop Category"; });
	}

	public override async Task HandleAsync(AddProductToCategoryRequest request, CancellationToken c) {
		var existing = await context.ProductCategories
			.Where(e => e.CategoryId == request.CategoryId)
			.Select(e => new { e.ProductId, e.Order })
			.AsNoTracking()
			.ToListAsync(c);

		if (existing.Exists(e => e.ProductId == (ulong)request.ProductId)) {
			await Send.NoContentAsync(c);
			return;
		}

		if (existing.Count == 0) {
			var category = await context.Categories
				.AsNoTracking()
				.FirstOrDefaultAsync(e => e.Id == request.CategoryId, c);

			if (category is null) {
				await Send.NotFoundAsync(c);
				return;
			}
		}

		var product = await context.Products
			.AsNoTracking()
			.FirstOrDefaultAsync(p => p.Id == (ulong)request.ProductId, c);

		if (product is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var productCategory = new ProductCategory {
			CategoryId = request.CategoryId,
			ProductId = product.Id,
			Order = existing.Count + 1
		};

		context.ProductCategories.Add(productCategory);
		await context.SaveChangesAsync(c);

		await cacheStore.EvictByTagAsync("categories", c);

		await Send.NoContentAsync(c);
	}
}
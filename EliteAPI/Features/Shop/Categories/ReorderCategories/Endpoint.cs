using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.Common;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.ReorderCategories;

internal sealed class ReorderCategoriesEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<ReorderIntRequest> {
	
	public override void Configure() {
		Post("/shop/categories/reorder");
		Policies(ApiUserPolicies.Admin);
		Version(0);
		
		Description(x => x.Accepts<ReorderIntRequest>());

		Summary(s => {
			s.Summary = "Reorder Shop Categories";
		});
	}

	public override async Task HandleAsync(ReorderIntRequest request, CancellationToken c) {
		var categories = await context.Categories
			.Select(e => new { e.Id, e.Order })
			.ToListAsync(cancellationToken: c);
		
		if (request.Elements.Select(e => e.Id).Except(categories.Select(e => e.Id)).Any()) {
			ThrowError("Invalid category id.");
		}
		
		var ordered = request.Elements.OrderBy(o => o.Order).ToList();
		try {
			for (var i = 0; i < ordered.Count; i++) {
				var order = ordered[i];
				var category = categories.FirstOrDefault(e => e.Id == order.Id);
			
				if (category is null) {
					ThrowError("Invalid category id.");
				}
			
				if (category.Order == i) {
					continue;
				}
			
				await context.Categories
					.Where(e => e.Id == category.Id)
					.ExecuteUpdateAsync(e => e.SetProperty(x => x.Order, i), cancellationToken: c);
			}
		} catch {
			ThrowError("Failed to reorder categories.");
		}
		
		await cacheStore.EvictByTagAsync("categories", c);

		await SendNoContentAsync(cancellation: c);
	}
}
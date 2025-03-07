using EliteAPI.Data;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.DeleteCategory;

internal sealed class DeleteCategoryRequest {
	/// <summary>
	/// Id of the category to delete
	/// </summary>
	public required int CategoryId { get; set; }
}

internal sealed class UpdateCategoryEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<DeleteCategoryRequest> {
	
	public override void Configure() {
		Delete("/shop/category/{CategoryId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => {
			s.Summary = "Delete Shop Category";
		});
	}

	public override async Task HandleAsync(DeleteCategoryRequest request, CancellationToken c) {
		var category = await context.Categories
			.FirstOrDefaultAsync(e => e.Id == request.CategoryId, cancellationToken: c);
		
		if (category is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		context.Categories.Remove(category);
		
		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("categories", c);

		await SendNoContentAsync(cancellation: c);
	}
}
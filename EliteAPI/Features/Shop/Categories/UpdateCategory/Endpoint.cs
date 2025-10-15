using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Outgoing.Shop;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.UpdateCategory;

internal sealed class UpdateCategoryRequest
{
	/// <summary>
	/// Id of the category to update
	/// </summary>
	public required int CategoryId { get; set; }

	[FromBody] public required EditCategoryDto CategoryData { get; set; }
}

internal sealed class UpdateCategoryEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<UpdateCategoryRequest>
{
	public override void Configure() {
		Patch("/shop/category/{CategoryId}");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => { s.Summary = "Update Shop Category"; });
	}

	public override async Task HandleAsync(UpdateCategoryRequest request, CancellationToken c) {
		var category = await context.Categories
			.FirstOrDefaultAsync(e => e.Id == request.CategoryId, c);

		if (category is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var incoming = request.CategoryData;

		category.Title = incoming.Title ?? category.Title;
		category.Description = incoming.Description ?? category.Description;
		category.Published = incoming.Published ?? category.Published;

		if (incoming.Slug is not null && incoming.Slug != category.Slug) {
			var existing = await context.Categories
				.AsNoTracking()
				.FirstOrDefaultAsync(e => e.Slug == incoming.Slug, c);

			if (existing is not null) ThrowError("A category with that slug already exists.");

			category.Slug = incoming.Slug;
		}

		await context.SaveChangesAsync(c);
		await cacheStore.EvictByTagAsync("categories", c);

		await Send.NoContentAsync(c);
	}
}
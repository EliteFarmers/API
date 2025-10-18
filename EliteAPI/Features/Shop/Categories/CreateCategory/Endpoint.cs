using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Outgoing.Shop;
using EliteAPI.Models.Entities.Monetization;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.CreateCategory;

internal sealed class CreateCategoryEndpoint(
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<CreateCategoryDto>
{
	public override void Configure() {
		Post("/shop/category");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Summary(s => { s.Summary = "Create Shop Category"; });
	}

	public override async Task HandleAsync(CreateCategoryDto request, CancellationToken c) {
		var existing = await context.Categories
			.AsNoTracking()
			.FirstOrDefaultAsync(e => e.Slug == request.Slug, c);

		if (existing is not null) ThrowError("A category with that slug already exists.");

		var category = new Category {
			Description = request.Description,
			Slug = request.Slug,
			Title = request.Title
		};

		context.Categories.Add(category);
		await context.SaveChangesAsync(c);

		await cacheStore.EvictByTagAsync("categories", c);

		await Send.NoContentAsync(c);
	}
}
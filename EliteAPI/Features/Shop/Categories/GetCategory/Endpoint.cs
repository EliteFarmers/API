using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Outgoing.Shop;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.GetCategory;

internal sealed class GetCategoryRequest {
	/// <summary>
	/// Category id or slug
	/// </summary>
	public required string Category { get; set; }
}

internal sealed class GetCategoryEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<GetCategoryRequest, ShopCategoryDto> {
	
	public override void Configure() {
		Get("/shop/category/{Category}");
		Options(o => o.WithMetadata(new OptionalAuthorizeAttribute()));
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Shop Category";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("categories"));
		});
	}

	public override async Task HandleAsync(GetCategoryRequest request, CancellationToken c) {
		var admin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);
		
		var query = context.Categories
			.Include(category => category.Products.Where(p => admin || p.Available))
			.Where(category => admin || category.Published);
		
		var category = int.TryParse(request.Category, out var categoryId)
			? await query.FirstOrDefaultAsync(e => e.Id == categoryId, cancellationToken: c)
			: await query.FirstOrDefaultAsync(e => e.Slug == request.Category, cancellationToken: c);
		
		if (category is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		category.Products = category.Products
			.OrderBy(p => p.ProductCategories.First(pc => pc.CategoryId == category.Id).Order)
			.ToList();
		
		var result = mapper.Map<ShopCategoryDto>(category);
		
		await Send.OkAsync(result, cancellation: c);
	}
}
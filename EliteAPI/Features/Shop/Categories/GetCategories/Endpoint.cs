using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing.Shop;
using EliteAPI.Models.Entities.Accounts;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Categories.GetCategories;

internal sealed class GetCategoriesEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<GetCategoriesRequest, List<ShopCategoryDto>> {
	
	public override void Configure() {
		Get("/shop/categories");
		Options(o => o.WithMetadata(new OptionalAuthorizeAttribute()));
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Shop Categories";
		});
		
		Description(d => d.AutoTagOverride("Category"));
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("categories"));
		});
	}

	public override async Task HandleAsync(GetCategoriesRequest request, CancellationToken c) {
		var admin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);

		if (request.IncludeProducts is true) {
			var results = await context.Categories
				.Include(category => category.Products.Where(p => admin || p.Available))
				.ThenInclude(product => product.ProductCategories)
				.OrderBy(category => category.Order)
				.Where(category => admin || category.Published)
				.ToListAsync(cancellationToken: c);
			
			foreach (var category in results) {
				category.Products = category.Products
					.OrderBy(p => p.ProductCategories.First(pc => pc.CategoryId == category.Id).Order)
					.ToList();
			}
			
			var result = mapper.Map<List<ShopCategoryDto>>(results);
			
			await SendAsync(result, cancellation: c);
		} else {
			var result = await context.Categories
				.OrderBy(category => category.Order)
				.Where(category => admin || category.Published)
				.Select(category => mapper.Map<ShopCategoryDto>(category))
				.ToListAsync(cancellationToken: c);
			
			await SendAsync(result, cancellation: c);
		}
	}
}
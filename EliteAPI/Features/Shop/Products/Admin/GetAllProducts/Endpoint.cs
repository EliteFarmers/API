using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Products.Admin.GetAllProducts;

internal sealed class GetAllProductsEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : EndpointWithoutRequest<List<ProductDto>> {
	
	public override void Configure() {
		Get("/products/admin");
		Policies(ApiUserPolicies.Moderator);
		Version(0);

		Summary(s => {
			s.Summary = "Get Admin Shop Products";
		});
		
		Description(d => d.AutoTagOverride("Product"));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = await context.Products
			.Include(p => p.WeightStyles)
			.Include(p => p.Images)
			.Select(x => mapper.Map<ProductDto>(x))
			.ToListAsync(cancellationToken: c);
		
		await Send.OkAsync(result, cancellation: c);
	}
}
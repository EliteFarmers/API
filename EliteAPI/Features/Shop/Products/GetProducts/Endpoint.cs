using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Products.GetProducts;

internal sealed class GetProductsEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : EndpointWithoutRequest<List<ProductDto>> {
	
	public override void Configure() {
		Get("/products");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Shop Products";
		});
		
		Description(d => d.AutoTagOverride("Product"));
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("products"));
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = await context.Products
			.Include(p => p.WeightStyles)
			.Include(p => p.Images)
			.Where(p => p.Available)
			.Select(x => mapper.Map<ProductDto>(x))
			.ToListAsync(cancellationToken: c);
		
		await SendAsync(result, cancellation: c);
	}
}
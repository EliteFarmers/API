using EliteAPI.Data;
using EliteAPI.Features.Account.DTOs;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Products.GetProduct;

internal sealed class GetProductEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, ProductDto> {
	
	public override void Configure() {
		Get("/product/{DiscordId}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Shop Product";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("products"));
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var result = await context.Products
			.Where(p => p.Id == request.DiscordIdUlong)
			.Include(p => p.WeightStyles)
			.Include(p => p.Images)
			.Select(x => mapper.Map<ProductDto>(x))
			.FirstOrDefaultAsync(cancellationToken: c);
		
		if (result is null) {
			await SendNotFoundAsync(cancellation: c);
			return;
		}
		
		await SendAsync(result, cancellation: c);
	}
}
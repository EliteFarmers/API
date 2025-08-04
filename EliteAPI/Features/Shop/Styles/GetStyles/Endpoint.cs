using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.GetStyles;

internal sealed class GetStylesEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : EndpointWithoutRequest<List<WeightStyleWithDataDto>> {
	
	public override void Configure() {
		Get("/product/styles");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Shop Styles";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("styles"));
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = await context.WeightStyles
			.Select(s => mapper.Map<WeightStyleWithDataDto>(s))
			.ToListAsync(cancellationToken: c);
		
		await Send.OkAsync(result, cancellation: c);
	}
}
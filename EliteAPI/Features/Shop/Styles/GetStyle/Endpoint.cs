using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.GetStyle;

internal sealed class GetStyleRequest {
	public int StyleId { get; set; }
}

internal sealed class GetStyleEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<GetStyleRequest, WeightStyleWithDataDto> {
	
	public override void Configure() {
		Get("/product/style/{StyleId}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Shop Style";
		});
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(1)).Tag("styles"));
		});
	}

	public override async Task HandleAsync(GetStyleRequest request, CancellationToken c) {
		var existing = await context.WeightStyles
			.Include(s => s.Images)
			.Where(s => s.Id == request.StyleId)
			.FirstOrDefaultAsync(cancellationToken: c);
		
		if (existing is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var result = mapper.Map<WeightStyleWithDataDto>(existing);
		await Send.OkAsync(result, cancellation: c);
	}
}
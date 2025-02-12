using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Shop.Styles.GetStyle;

internal sealed class Request {
	public int StyleId { get; set; }
}

internal sealed class GetStyleEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<Request, WeightStyleWithDataDto> {
	
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

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var existing = await context.WeightStyles
			.Include(s => s.Images)
			.Where(s => s.Id == request.StyleId)
			.FirstOrDefaultAsync(cancellationToken: c);
		
		if (existing is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var result = mapper.Map<WeightStyleWithDataDto>(existing);
		await SendAsync(result, cancellation: c);
	}
}
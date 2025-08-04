using EliteAPI.Data;
using EliteAPI.Models.DTOs.Incoming;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Badges.GetBadges;

internal sealed class GetBadgesEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
	) : EndpointWithoutRequest<List<BadgeDto>>
{
	public override void Configure() {
		Get("/badges");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get all badges";
		});
		
		Description(x => x.AutoTagOverride("Badge"));
		
		Options(o => {
			o.CacheOutput(c => c.Expire(TimeSpan.FromHours(2)).Tag("badges"));
		});
	}

	public override async Task HandleAsync(CancellationToken c) {
		var badges = context.Badges
			.AsNoTracking()
			.ToList();
        
		await Send.OkAsync(mapper.Map<List<BadgeDto>>(badges), cancellation: c);
	}
}
using EliteAPI.Data;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Guilds.Public.GetGuilds;

internal sealed class GetPublicGuildsEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : EndpointWithoutRequest<List<GuildDetailsDto>> {
	public override void Configure() {
		Get("/guilds");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get public guilds"; });

		Description(d => d.AutoTagOverride("Guild"));
		Options(o => { o.CacheOutput(c => c.Expire(TimeSpan.FromHours(2)).Tag("guilds")); });
	}

	public override async Task HandleAsync(CancellationToken c) {
		var guilds = await context.Guilds
			.Where(g => g.InviteCode != null && g.IsPublic)
			.OrderByDescending(g => g.MemberCount)
			.Select(g => mapper.Map<GuildDetailsDto>(g))
			.ToListAsync(c);

		await Send.OkAsync(guilds, c);
	}
}
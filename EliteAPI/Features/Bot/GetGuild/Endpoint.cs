using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Bot.GetGuild;

internal sealed class GetBotGuildEndpoint(
	DataContext context,
	AutoMapper.IMapper mapper
) : Endpoint<DiscordIdRequest, PrivateGuildDto>
{
	public override void Configure() {
		Get("/bot/{DiscordId}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => { s.Summary = "Get guild"; });
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var guild = await context.Guilds.FirstOrDefaultAsync(g => g.Id == request.DiscordIdUlong, c);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		await Send.OkAsync(mapper.Map<PrivateGuildDto>(guild), c);
	}
}
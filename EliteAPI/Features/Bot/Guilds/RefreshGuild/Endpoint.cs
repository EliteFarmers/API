using EliteAPI.Authentication;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.RefreshGuild;

internal sealed class RefreshGuildEndpoint(
	IDiscordService discordService
) : Endpoint<DiscordIdRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);
		
		Description(x => x.Accepts<DiscordIdRequest>());

		Summary(s => {
			s.Summary = "Request Guild Update";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		await discordService.RefreshDiscordGuild(request.DiscordIdUlong);
		await SendNoContentAsync(cancellation: c);
	}
}
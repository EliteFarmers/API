using EliteAPI.Authentication;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuild;

internal sealed class UpdateGuildEndpoint(
	IGuildService guildService
) : Endpoint<BotUpdateGuildRequest, DiscordIdRequest> {
	
	public override void Configure() {
		Patch("/bot/guild/{DiscordId}");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => {
			s.Summary = "Update Guild";
		});
	}

	public override async Task HandleAsync(BotUpdateGuildRequest request, CancellationToken c) {
		await guildService.UpdateGuildData(request.DiscordIdUlong, request.Guild);
		await SendNoContentAsync(cancellation: c);
	}
}
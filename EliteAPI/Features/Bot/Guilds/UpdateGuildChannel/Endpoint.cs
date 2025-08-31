using EliteAPI.Authentication;
using EliteAPI.Features.Guilds.Services;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildChannel;

internal sealed class UpdateGuildChannelEndpoint(
	IGuildService guildService
) : Endpoint<BotUpdateGuildChannelRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}/channels");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => {
			s.Summary = "Update Guild Channel";
		});
	}

	public override async Task HandleAsync(BotUpdateGuildChannelRequest request, CancellationToken c) {
		await guildService.UpdateGuildChannelData(request.DiscordIdUlong, request.Channel);
		await Send.NoContentAsync(cancellation: c);
	}
}
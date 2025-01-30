using EliteAPI.Authentication;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildChannel;

internal sealed class UpdateGuildChannelEndpoint(
	IGuildService guildService
) : Endpoint<BotUpdateGuildChannelRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}/channels");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Update Guild Channel";
		});
	}

	public override async Task HandleAsync(BotUpdateGuildChannelRequest request, CancellationToken c) {
		await guildService.UpdateGuildChannelData(request.DiscordIdUlong, request.Channel);
		await SendNoContentAsync(c);
	}
}
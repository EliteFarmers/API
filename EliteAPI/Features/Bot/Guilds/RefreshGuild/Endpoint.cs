using EliteAPI.Authentication;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Guilds.RefreshGuild;

internal sealed class RefreshGuildEndpoint(
	IDiscordService discordService
) : Endpoint<DiscordIdRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Request Guild Update";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		await discordService.RefreshDiscordGuild(request.DiscordIdUlong);
		await SendOkAsync(cancellation: c);
	}
}
using EliteAPI.Authentication;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuild;

internal sealed class UpdateGuildEndpoint(
	IGuildService guildService
) : Endpoint<BotUpdateGuildRequest, DiscordIdRequest> {
	
	public override void Configure() {
		Patch("/bot/guild/{DiscordId}");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
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
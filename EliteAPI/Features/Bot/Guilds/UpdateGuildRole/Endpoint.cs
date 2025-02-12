using EliteAPI.Authentication;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildRole;

internal sealed class UpdateGuildRoleEndpoint(
	IGuildService guildService
) : Endpoint<BotUpdateGuildRoleRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}/roles");
		Options(o => o.WithMetadata(new ServiceFilterAttribute(typeof(DiscordBotOnlyFilter))));
		Version(0);

		Summary(s => {
			s.Summary = "Update Guild Role";
		});
	}

	public override async Task HandleAsync(BotUpdateGuildRoleRequest request, CancellationToken c) {
		await guildService.UpdateGuildRoleData(request.DiscordIdUlong, request.Role);
		await SendOkAsync(cancellation: c);
	}
}
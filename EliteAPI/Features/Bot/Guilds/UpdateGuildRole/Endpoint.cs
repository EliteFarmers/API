using EliteAPI.Authentication;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Bot.Guilds.UpdateGuildRole;

internal sealed class UpdateGuildRoleEndpoint(
	IGuildService guildService
) : Endpoint<BotUpdateGuildRoleRequest> {
	
	public override void Configure() {
		Post("/bot/guild/{DiscordId}/roles");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => {
			s.Summary = "Update Guild Role";
		});
	}

	public override async Task HandleAsync(BotUpdateGuildRoleRequest request, CancellationToken c) {
		await guildService.UpdateGuildRoleData(request.DiscordIdUlong, request.Role);
		await Send.NoContentAsync(cancellation: c);
	}
}
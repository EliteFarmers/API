using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.Admin.SetLocked;

internal sealed class SetGuildLockedEndpoint(
	IDiscordService discordService,
	DataContext context)
	: Endpoint<SetGuildLockedRequest> {
	public override void Configure() {
		Post("/guild/{DiscordId}/lock");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(x => x.Accepts<SetGuildLockedRequest>());

		Summary(s => { s.Summary = "Lock or unlock a guild"; });
	}

	public override async Task HandleAsync(SetGuildLockedRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		guild.Features.Locked = request.Locked ?? true;

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await Send.NoContentAsync(c);
	}
}
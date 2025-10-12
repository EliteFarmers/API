using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Guilds.Admin.SetPublic;

internal sealed class SetGuildPublicEndpoint(
	IDiscordService discordService,
	IOutputCacheStore cacheStore,
	DataContext context)
	: Endpoint<SetGuildPublicRequest> {
	public override void Configure() {
		Post("/guild/{DiscordId}/public");
		Policies(ApiUserPolicies.Admin);
		Version(0);

		Description(x => x.Accepts<SetGuildPublicRequest>());

		Summary(s => { s.Summary = "Set a guild to public or private"; });
	}

	public override async Task HandleAsync(SetGuildPublicRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		guild.IsPublic = request.Public ?? true;

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await cacheStore.EvictByTagAsync("guilds", c);

		await Send.NoContentAsync(c);
	}
}
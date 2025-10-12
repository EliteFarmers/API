using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Bot.GetJacobFeature;

internal sealed class GetJacobFeatureEndpoint(
	DataContext context
) : Endpoint<DiscordIdRequest, GuildJacobLeaderboardFeature> {
	public override void Configure() {
		Get("/bot/{DiscordId}/jacob");
		Options(o => o.AddEndpointFilter<DiscordBotOnlyFilter>());
		AllowAnonymous(); // Auth done in endpoint filter
		Version(0);

		Summary(s => { s.Summary = "Get guild jacob"; });
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var guild = await context.Guilds.FirstOrDefaultAsync(g => g.Id == request.DiscordIdUlong, c);
		if (guild is null || !guild.Features.JacobLeaderboardEnabled) {
			await Send.NotFoundAsync(c);
			return;
		}

		if (guild.Features.JacobLeaderboard is not null) {
			await Send.OkAsync(guild.Features.JacobLeaderboard, c);
			return;
		}

		guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await Send.OkAsync(guild.Features.JacobLeaderboard, c);
	}
}
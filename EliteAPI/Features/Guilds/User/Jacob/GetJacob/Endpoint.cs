using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.GetJacob;

internal sealed class GetGuildJacobEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<DiscordIdRequest, GuildJacobLeaderboardFeature> {
	
	public override void Configure() {
		Get("/user/guild/{DiscordId}/jacob");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Get Jacob leaderboards for a guild";
		});
	}

	public override async Task HandleAsync(DiscordIdRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}

		if (!guild.Features.JacobLeaderboardEnabled) {
			await SendNotFoundAsync(c);
			return;
		}

		if (guild.Features.JacobLeaderboard is not null) {
			await SendAsync(guild.Features.JacobLeaderboard, cancellation: c);
			return;
		}
        
		guild.Features.JacobLeaderboard = new GuildJacobLeaderboardFeature();
		context.Guilds.Update(guild);
		
		await context.SaveChangesAsync(c);
		
		await SendAsync(guild.Features.JacobLeaderboard, cancellation: c);
	}
}
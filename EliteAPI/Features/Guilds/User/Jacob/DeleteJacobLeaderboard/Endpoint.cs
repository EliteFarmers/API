using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.DeleteJacobLeaderboard;

internal sealed class DeleteGuildJacobFeatureEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<DeleteGuildJacobLeaderboardRequest> {
	
	public override void Configure() {
		Delete("/user/guild/{DiscordId}/jacob/{LeaderboardId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Delete a Jacob leaderboard";
		});
	}

	public override async Task HandleAsync(DeleteGuildJacobLeaderboardRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild is null) {
			await SendNotFoundAsync(c);
			return;
		}

		if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var feature = guild.Features.JacobLeaderboard;
		var existing = feature.Leaderboards.FirstOrDefault(lb => lb.Id.Equals(request.LeaderboardId));
        
		if (existing is null) {
			await SendNotFoundAsync(c);
			return;
		}

		feature.Leaderboards.Remove(existing);

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendNoContentAsync(cancellation: c);
	}
}
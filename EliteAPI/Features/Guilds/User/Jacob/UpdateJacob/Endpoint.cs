using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.UpdateJacob;

internal sealed class UpdateGuildJacobFeatureEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<UpdateJacobFeatureRequest> {
	
	public override void Configure() {
		Patch("/user/guild/{DiscordId}/jacob");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Update Jacob leaderboards for a guild";
		});
	}

	public override async Task HandleAsync(UpdateJacobFeatureRequest request, CancellationToken c) {
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

		feature.BlockedRoles = request.BlockedRoles;
		feature.BlockedUsers = request.BlockedUsers;
		feature.RequiredRoles = request.RequiredRoles;
		feature.ExcludedParticipations = request.ExcludedParticipations;
		feature.ExcludedTimespans = request.ExcludedTimespans;
        
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendOkAsync(cancellation: c);
	}
}
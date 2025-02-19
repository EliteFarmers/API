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

		feature.BlockedRoles = request.Feature.BlockedRoles;
		feature.BlockedUsers = request.Feature.BlockedUsers;
		feature.RequiredRoles = request.Feature.RequiredRoles;
		feature.ExcludedParticipations = request.Feature.ExcludedParticipations;
		feature.ExcludedTimespans = request.Feature.ExcludedTimespans;
        
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendNoContentAsync(cancellation: c);
	}
}
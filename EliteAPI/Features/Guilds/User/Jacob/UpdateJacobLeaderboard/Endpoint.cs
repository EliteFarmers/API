using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.UpdateJacobLeaderboard;

internal sealed class UpdateGuildJacobFeatureEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<UpdateJacobLeaderboardRequest> {
	
	public override void Configure() {
		Patch("/user/guild/{DiscordId}/jacob/{LeaderboardId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Update a Jacob leaderboard";
		});
	}

	public override async Task HandleAsync(UpdateJacobLeaderboardRequest request, CancellationToken c) {
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

		existing.EndCutoff = request.EndCutoff ?? existing.EndCutoff;
		existing.StartCutoff = request.StartCutoff ?? existing.StartCutoff;
		existing.ChannelId = request.ChannelId ?? existing.ChannelId;
		existing.Title = request.Title ?? existing.Title;
		existing.PingForSmallImprovements = request.PingForSmallImprovements ?? existing.PingForSmallImprovements;
		existing.RequiredRole = request.RequiredRole ?? existing.RequiredRole;
		existing.BlockedRole = request.BlockedRole ?? existing.BlockedRole;
		existing.UpdateChannelId = request.UpdateChannelId ?? existing.UpdateChannelId;
		existing.UpdateRoleId = request.UpdateRoleId ?? existing.UpdateRoleId;
		
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendNoContentAsync(c);
	}
}
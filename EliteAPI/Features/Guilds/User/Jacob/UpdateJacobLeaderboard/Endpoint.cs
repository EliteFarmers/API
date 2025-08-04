using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
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
			await Send.NotFoundAsync(c);
			return;
		}

		if (!guild.Features.JacobLeaderboardEnabled || guild.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var feature = guild.Features.JacobLeaderboard;
		var existing = feature.Leaderboards.FirstOrDefault(lb => lb.Id.Equals(request.LeaderboardId));
		
		if (existing is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var lb = request.Leaderboard;
		existing.EndCutoff = lb.EndCutoff ?? existing.EndCutoff;
		existing.StartCutoff = lb.StartCutoff ?? existing.StartCutoff;
		existing.ChannelId = lb.ChannelId ?? existing.ChannelId;
		existing.Title = lb.Title ?? existing.Title;
		existing.PingForSmallImprovements = lb.PingForSmallImprovements ?? existing.PingForSmallImprovements;
		existing.RequiredRole = lb.RequiredRole ?? existing.RequiredRole;
		existing.BlockedRole = lb.BlockedRole ?? existing.BlockedRole;
		existing.UpdateChannelId = lb.UpdateChannelId ?? existing.UpdateChannelId;
		existing.UpdateRoleId = lb.UpdateRoleId ?? existing.UpdateRoleId;
		
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await Send.NoContentAsync(cancellation: c);
	}
}
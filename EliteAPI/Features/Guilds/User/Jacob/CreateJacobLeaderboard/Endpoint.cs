using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.CreateJacobLeaderboard;

internal sealed class UpdateGuildJacobFeatureEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<CreateJacobLeaderboardRequest> {
	
	public override void Configure() {
		Post("/user/guild/{DiscordId}/jacob/leaderboard");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Create a Jacob leaderboard";
		});
	}

	public override async Task HandleAsync(CreateJacobLeaderboardRequest request, CancellationToken c) {
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
        
		if (feature.Leaderboards.Count >= feature.MaxLeaderboards) {
			ThrowError("You have reached the maximum amount of leaderboards.", StatusCodes.Status400BadRequest);
		}

		var leaderboard = new GuildJacobLeaderboard {
			Id = (guild.Id + (ulong)Random.Shared.Next(1000000, 9000000)).ToString(),
			Title = request.Title,
			ChannelId = request.ChannelId,
			StartCutoff = request.StartCutoff ?? -1,
			EndCutoff = request.EndCutoff ?? -1,
			Active = request.Active ?? true,
			RequiredRole = request.RequiredRole,
			BlockedRole = request.BlockedRole,
			UpdateChannelId = request.UpdateChannelId,
			UpdateRoleId = request.UpdateRoleId,
			PingForSmallImprovements = request.PingForSmallImprovements ?? false,
		};
        
		if (feature.Leaderboards.Any(l => l.Id.Equals(leaderboard.Id))) {
			ThrowError("Leaderboard already exists", StatusCodes.Status400BadRequest);
		}
        
		feature.Leaderboards.Add(leaderboard);
		
		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await SendNoContentAsync(c);
	}
}
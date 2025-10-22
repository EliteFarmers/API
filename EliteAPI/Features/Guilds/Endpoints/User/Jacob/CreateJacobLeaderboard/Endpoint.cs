using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.CreateJacobLeaderboard;

internal sealed class CreateGuildJacobLeaderboardEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<CreateJacobLeaderboardRequest>
{
	public override void Configure() {
		Post("/user/guild/{DiscordId}/jacob/leaderboard");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => { s.Summary = "Create a Jacob leaderboard"; });
	}

	public override async Task HandleAsync(CreateJacobLeaderboardRequest request, CancellationToken c) {
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

		if (feature.Leaderboards.Count >= feature.MaxLeaderboards)
			ThrowError("You have reached the maximum amount of leaderboards.", StatusCodes.Status400BadRequest);

		var lb = request.Leaderboard;
		var leaderboard = new GuildJacobLeaderboard {
			Id = (guild.Id + (ulong)Random.Shared.Next(1000000, 9000000)).ToString(),
			Title = lb.Title,
			ChannelId = lb.ChannelId,
			StartCutoff = lb.StartCutoff ?? -1,
			EndCutoff = lb.EndCutoff ?? -1,
			Active = lb.Active ?? true,
			RequiredRole = lb.RequiredRole,
			BlockedRole = lb.BlockedRole,
			UpdateChannelId = lb.UpdateChannelId,
			UpdateRoleId = lb.UpdateRoleId,
			PingForSmallImprovements = lb.PingForSmallImprovements ?? false
		};

		if (feature.Leaderboards.Any(l => l.Id.Equals(leaderboard.Id)))
			ThrowError("Leaderboard already exists", StatusCodes.Status400BadRequest);

		feature.Leaderboards.Add(leaderboard);

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);

		await Send.NoContentAsync(c);
	}
}
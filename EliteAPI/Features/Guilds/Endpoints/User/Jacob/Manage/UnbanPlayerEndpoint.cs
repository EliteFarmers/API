using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class UnbanPlayerRequest : JacobManageRequest
{
	public required string PlayerUuid { get; set; }
}

internal sealed class UnbanPlayerEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<UnbanPlayerRequest>
{
	public override void Configure() {
		Delete("/user/guild/{DiscordId}/jacob/bans/{PlayerUuid}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		Summary(s => { s.Summary = "Unban a player from Jacob leaderboards"; });
	}

	public override async Task HandleAsync(UnbanPlayerRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild?.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;

		if (!feature.BlockedPlayerUuids.Remove(request.PlayerUuid)) {
			ThrowError("Player ban not found.", StatusCodes.Status404NotFound);
			return;
		}

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}
}

using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class UnbanParticipationRequest : JacobManageRequest
{
	public required string ParticipationId { get; set; }
}

internal sealed class UnbanParticipationFromJacobLeaderboardEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<UnbanParticipationRequest>
{
	public override void Configure() {
		Delete("/guilds/{DiscordId}/jacob/bans/participations/{ParticipationId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		Summary(s => { s.Summary = "Unban a specific participation"; });
	}

	public override async Task HandleAsync(UnbanParticipationRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild?.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;

		if (!feature.ExcludedParticipations.Remove(request.ParticipationId)) {
			ThrowError("Participation ban not found.", StatusCodes.Status404NotFound);
			return;
		}

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}
}
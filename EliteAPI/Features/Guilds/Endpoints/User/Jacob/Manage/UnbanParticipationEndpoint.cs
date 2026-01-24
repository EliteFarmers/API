using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

internal sealed class UnbanParticipationEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<UnbanParticipationRequest>
{
	public override void Configure() {
		Delete("/user/guild/{DiscordId}/jacob/participation/{ParticipationId}");
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

public class UnbanParticipationRequest : JacobManageRequest
{
	public required string ParticipationId { get; set; }
}
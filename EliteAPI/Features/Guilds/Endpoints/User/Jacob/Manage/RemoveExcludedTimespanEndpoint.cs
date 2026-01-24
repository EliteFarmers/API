using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class RemoveExcludedTimespanRequest : JacobManageRequest
{
	[FromBody] 
	public required RemoveExcludedTimespanRequestBody Body { get; set; }
	
	public class RemoveExcludedTimespanRequestBody
	{
		public required long Start { get; set; }
		public required long End { get; set; }
	}
}

internal sealed class RemoveExcludedTimespanEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<RemoveExcludedTimespanRequest>
{
	public override void Configure() {
		Delete("/user/guild/{DiscordId}/jacob/timespan");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		Summary(s => { s.Summary = "Remove an excluded timespan"; });
	}

	public override async Task HandleAsync(RemoveExcludedTimespanRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild?.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;
		var removed = feature.ExcludedTimespans.RemoveAll(t => t.Start == request.Body.Start && t.End == request.Body.End);

		if (removed == 0) {
			ThrowError("Timespan not found.", StatusCodes.Status404NotFound);
			return;
		}

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}
}
using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Account.Services;
using EliteAPI.Models.Entities.Discord;
using FastEndpoints;

namespace EliteAPI.Features.Guilds.User.Jacob.Manage;

public class AddExcludedTimespanRequest : JacobManageRequest
{
	[FromBody]
	public AddExcludedTimespanRequestBody Body { get; set; } = null!;
	
	public class AddExcludedTimespanRequestBody
	{	
		public required long Start { get; set; }
		public required long End { get; set; }
		public string? Reason { get; set; }
	}
}

internal sealed class AddExcludedTimespanEndpoint(
	IDiscordService discordService,
	DataContext context
) : Endpoint<AddExcludedTimespanRequest>
{
	public override void Configure() {
		Post("/user/guild/{DiscordId}/jacob/timespan");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);
		Summary(s => { s.Summary = "Add an excluded timespan"; });
	}

	public override async Task HandleAsync(AddExcludedTimespanRequest request, CancellationToken c) {
		var guild = await discordService.GetGuild(request.DiscordIdUlong);
		if (guild?.Features.JacobLeaderboard is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var feature = guild.Features.JacobLeaderboard;

		if (feature.ExcludedTimespans.Any(t => t.Start == request.Body.Start && t.End == request.Body.End)) {
			ThrowError("Timespan already exists.", StatusCodes.Status409Conflict);
			return;
		}

		feature.ExcludedTimespans.Add(new ExcludedTimespan {
			Start = request.Body.Start,
			End = request.Body.End,
			Reason = request.Body.Reason
		});

		context.Guilds.Update(guild);
		await context.SaveChangesAsync(c);
		await Send.NoContentAsync(c);
	}
}
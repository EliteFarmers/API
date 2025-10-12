using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.KickTeamMember;

internal sealed class KickTeamMemberRequest : PlayerRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
}

internal sealed class KickTeamMemberAdminEndpoint(
	IEventTeamService teamService,
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<KickTeamMemberRequest> {
	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}/teams/{TeamId}/members/{Player}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Description(s => s.Accepts<KickTeamMemberRequest>());

		Summary(s => { s.Summary = "Kick an Event Team Member"; });
	}

	public override async Task HandleAsync(KickTeamMemberRequest request, CancellationToken c) {
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, c);

		if (@event is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		var team = await context.EventTeams
			.AsNoTracking()
			.Where(team => team.EventId == @event.Id && team.Id == request.TeamId)
			.FirstOrDefaultAsync(c);

		if (team is null) {
			await Send.NotFoundAsync(c);
			return;
		}

		await teamService.KickMemberAsync(request.TeamId, request.Player);

		await cacheStore.EvictByTagAsync("event-teams", c);
		await Send.NoContentAsync(c);
	}
}

internal sealed class KickTeamMemberRequestValidator : Validator<KickTeamMemberRequest> {
	public KickTeamMemberRequestValidator() {
		Include(new PlayerRequestValidator());
	}
}
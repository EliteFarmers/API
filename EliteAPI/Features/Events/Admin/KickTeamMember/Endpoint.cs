using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.KickTeamMember;

internal sealed class Request : PlayerRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
}

internal sealed class KickTeamMemberEndpoint(
	IEventTeamService teamService,
	DataContext context
) : Endpoint<Request> {

	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}/teams/{TeamId}/members/{Player}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Kick an Event Team Member";
		});
	}

	public override async Task HandleAsync(Request request, CancellationToken c) {
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, cancellationToken: c);
		
		if (@event is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var team = await context.EventTeams
			.AsNoTracking()
			.Where(team => team.EventId == @event.Id && team.Id == request.TeamId)
			.FirstOrDefaultAsync(cancellationToken: c);
        
		if (team is null) {
			await SendNotFoundAsync(c);
			return;
		}

		await teamService.KickMemberAsync(request.TeamId, request.Player);

		await SendOkAsync(cancellation: c);
	}
}

internal sealed class KickTeamMemberRequestValidator : Validator<Request> {
	public KickTeamMemberRequestValidator() {
		Include(new PlayerRequestValidator());
	}
}
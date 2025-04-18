using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.DeleteTeam;

internal sealed class DeleteTeamRequest : DiscordIdRequest {
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
}

internal sealed class DeleteTeamEndpoint(
	IEventTeamService teamService,
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<DeleteTeamRequest> {

	public override void Configure() {
		Delete("/guild/{DiscordId}/events/{EventId}/teams/{TeamId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Delete an Event Team";
		});
	}

	public override async Task HandleAsync(DeleteTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordIdUlong, cancellationToken: c);
		
		if (userId is null || @event is null) {
			await SendUnauthorizedAsync(c);
			return;
		}
		
		var team = await context.EventTeams
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.EventId == @event.Id && t.Id == request.TeamId, cancellationToken: c);

		if (team is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var result = await teamService.DeleteTeamAsync(request.TeamId);
		
		if (result is BadRequestObjectResult badRequest) {
			await SendAsync(badRequest.Value?.ToString(), cancellation: c);
			return;
		}
		
		await cacheStore.EvictByTagAsync("event-teams", c);
		await SendNoContentAsync(cancellation: c);
	}
}

internal sealed class DeleteTeamRequestValidator : Validator<DeleteTeamRequest> {
	public DeleteTeamRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}
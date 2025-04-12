using EliteAPI.Authentication;
using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.Admin.SetTeamOwner;

internal sealed class SetTeamOwnerRequest {
	public ulong DiscordId { get; set; }
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
	public required string Player { get; set; }
}

internal sealed class SetTeamOwnerEndpoint(
	IEventTeamService teamService,
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<SetTeamOwnerRequest> {

	public override void Configure() {
		Put("/guild/{DiscordId}/events/{EventId}/teams/{TeamId}/owner");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Set player as team owner";
		});
	}

	public override async Task HandleAsync(SetTeamOwnerRequest request, CancellationToken c) {
		var userId = User.GetId();
		var @event = await context.Events
			.FirstOrDefaultAsync(e => e.Id == request.EventId && e.GuildId == request.DiscordId, cancellationToken: c);
		
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
		
		var result = await teamService.SetTeamOwnerAsync(request.TeamId, request.Player);
		
		if (result is BadRequestObjectResult badRequest) {
			await SendAsync(badRequest.Value?.ToString(), cancellation: c);
			return;
		}

		await cacheStore.EvictByTagAsync("event-teams", c);
		await SendNoContentAsync(cancellation: c);
	}
}
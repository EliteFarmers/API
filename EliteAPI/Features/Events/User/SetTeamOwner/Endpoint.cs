using EliteAPI.Data;
using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Events.User.SetTeamOwner;

internal sealed class ChangeTeamOwnerRequest {
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
	public required string Player { get; set; }
}

internal sealed class SetTeamOwnerEndpoint(
	IEventTeamService teamService,
	DataContext context,
	IOutputCacheStore cacheStore
) : Endpoint<ChangeTeamOwnerRequest> {

	public override void Configure() {
		Put("/event/{EventId}/team/{TeamId}/owner");
		Version(0);
		
		Description(s => s.Accepts<ChangeTeamOwnerRequest>());

		Summary(s => {
			s.Summary = "Set player as team owner";
		});
	}

	public override async Task HandleAsync(ChangeTeamOwnerRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await SendUnauthorizedAsync(c);
			return;
		}
		
		var team = await context.EventTeams
			.AsNoTracking()
			.FirstOrDefaultAsync(t => t.EventId == request.EventId && t.Id == request.TeamId, cancellationToken: c);

		if (team is null) {
			await SendNotFoundAsync(c);
			return;
		}
		
		var result = await teamService.SetTeamOwnerValidateAsync(request.TeamId, userId, request.Player);
		
		if (result is BadRequestObjectResult badRequest) {
			await SendAsync(badRequest.Value?.ToString(), cancellation: c);
			return;
		}

		await cacheStore.EvictByTagAsync("event-teams", c);
		await SendNoContentAsync(cancellation: c);
	}
}
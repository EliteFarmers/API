using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.CreateTeam;

internal sealed class CreateTeamRequest
{
	[BindFrom("eventId")]
    public ulong EventId { get; set; }
    [FastEndpoints.FromBody]
    public required CreateEventTeamDto Team { get; set; }
}

internal sealed class CreateTeamEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<CreateTeamRequest>
{
	public override void Configure() {
		Post("/event/{EventId}/teams");
		Version(0);
		
		Description(x => x.Accepts<CreateTeamRequest>());

		Summary(s => {
			s.Summary = "Create a team";
		});
	}

	public override async Task HandleAsync(CreateTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await Send.UnauthorizedAsync(c);
			return;
		}

		var response = await teamService.CreateUserTeamAsync(request.EventId, request.Team, userId);

		if (response is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Failed to create team");
		}
		
		await cacheStore.EvictByTagAsync("event-teams", c);
		await Send.NoContentAsync(cancellation: c);
	}
}

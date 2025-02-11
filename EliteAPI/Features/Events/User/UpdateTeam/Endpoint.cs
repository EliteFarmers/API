using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.UpdateTeam;

internal sealed class UpdateTeamRequest
{
    public ulong EventId { get; set; }
    public int TeamId { get; set; }
    [FastEndpoints.FromBody]
    public required UpdateEventTeamDto Team { get; set; }
}

internal sealed class UpdateTeamEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<UpdateTeamRequest>
{
	public override void Configure() {
		Patch("/event/{EventId}/team/{TeamId}");
		Version(0);

		Summary(s => {
			s.Summary = "Update a team";
		});
	}

	public override async Task HandleAsync(UpdateTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await SendUnauthorizedAsync(c);
			return;
		}

		var admin = User.IsInRole(ApiUserPolicies.Admin) || User.IsInRole(ApiUserPolicies.Moderator);
		var response = await teamService.UpdateTeamAsync(request.TeamId, request.Team, userId, admin);

		if (response is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Failed to update team");
		}
		
		await cacheStore.EvictByTagAsync("event-teams", c);
		await SendNoContentAsync(cancellation: c);
	}
}

using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.LeaveTeam;

internal sealed class LeaveTeamRequest {
    public ulong EventId { get; set; }
    public int TeamId { get; set; }
}

internal sealed class LeaveTeamEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<LeaveTeamRequest>
{
	public override void Configure() {
		Post("/event/{EventId}/team/{TeamId}/leave");
		Version(0);

		Summary(s => {
			s.Summary = "Leave a team";
		});
	}

	public override async Task HandleAsync(LeaveTeamRequest request, CancellationToken c) {
        var userId = User.GetId();
        if (userId is null) {
            await SendUnauthorizedAsync(c);
            return;
        }
        
        var response = await teamService.LeaveTeamAsync(request.TeamId, userId);
        
        if (response is BadRequestObjectResult bad) {
	        ThrowError(bad.Value?.ToString() ?? "Failed to leave team");
        }
        
        await cacheStore.EvictByTagAsync("event-teams", c);
		await SendOkAsync(cancellation: c);
	}
}

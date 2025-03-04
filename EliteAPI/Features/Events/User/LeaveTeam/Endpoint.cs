using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.LeaveTeam;

internal sealed class LeaveTeamRequest {
	[BindFrom("eventId")]
    public ulong EventId { get; set; }
	[BindFrom("teamId")]
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

		Description(s => s.Accepts<LeaveTeamRequest>());

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
		await SendNoContentAsync(cancellation: c);
	}
}

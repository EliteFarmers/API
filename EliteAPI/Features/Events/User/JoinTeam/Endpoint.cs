using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.JoinTeam;

internal sealed class JoinTeamRequest {
    public ulong EventId { get; set; }
    public int TeamId { get; set; }
    /// <summary>
    /// Join code for the team
    /// </summary>
    [FastEndpoints.FromBody]
    public required string JoinCode { get; set; }
}

internal sealed class JoinTeamEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<JoinTeamRequest>
{
	public override void Configure() {
		Post("/event/{EventId}/team/{TeamId}/join");
		Version(0);

		Summary(s => {
			s.Summary = "Join a team";
		});
	}

	public override async Task HandleAsync(JoinTeamRequest request, CancellationToken c) {
        var userId = User.GetId();
        if (userId is null) {
            await SendUnauthorizedAsync(c);
            return;
        }
        
        var response = await teamService.JoinTeamValidateAsync(request.TeamId, userId, request.JoinCode);
        
        if (response is BadRequestObjectResult bad) {
	        ThrowError(bad.Value?.ToString() ?? "Failed to join team");
        }
        
        await cacheStore.EvictByTagAsync("event-teams", c);
		await SendOkAsync(cancellation: c);
	}
}

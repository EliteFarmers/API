using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.KickTeamMember;

internal sealed class KickTeamMemberEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<KickTeamMemberRequest>
{
	public override void Configure() {
		Delete("/event/{EventId}/team/{TeamId}/member/{Player}");
		Version(0);

		Description(s => s.Accepts<KickTeamMemberRequest>());
		
		Summary(s => {
			s.Summary = "Kick a team member";
			s.Description = "Kicked members can rejoin the team if they have the join code.";
		});
	}

	public override async Task HandleAsync(KickTeamMemberRequest request, CancellationToken c) {
        var userId = User.GetId();
        if (userId is null) {
            await Send.UnauthorizedAsync(c);
            return;
        }
        
        var response = await teamService.KickMemberValidateAsync(request.TeamId, userId, request.Player);
        
        if (response is BadRequestObjectResult bad) {
	        ThrowError(bad.Value?.ToString() ?? "Failed to leave team");
        }
        
        await cacheStore.EvictByTagAsync("event-teams", c);
		await Send.NoContentAsync(cancellation: c);
	}
}

using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.User.DeleteTeam;

internal sealed class DeleteTeamRequest
{
    public ulong EventId { get; set; }
    public int TeamId { get; set; }
}

internal sealed class DeleteTeamEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<DeleteTeamRequest>
{
	public override void Configure() {
		Delete("/event/{EventId}/team/{TeamId}");
		Version(0);

		Summary(s => {
			s.Summary = "Delete team";
		});
	}

	public override async Task HandleAsync(DeleteTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await SendUnauthorizedAsync(c);
			return;
		}

		var team = await teamService.GetTeamAsync(request.TeamId);
		if (team is null) {
			ThrowError("Invalid team id");
		}
		
		if (team.UserId != userId) {
			await SendUnauthorizedAsync(c);
			return;
		}
		
		var response = await teamService.DeleteTeamValidateAsync(request.TeamId);

		if (response is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Failed to generate new join code");
		}
		
		await cacheStore.EvictByTagAsync("event-teams", c);
		await SendOkAsync(cancellation: c);
	}
}

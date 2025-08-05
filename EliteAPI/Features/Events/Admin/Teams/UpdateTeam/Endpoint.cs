using EliteAPI.Authentication;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace EliteAPI.Features.Events.Admin.UpdateTeam;

internal sealed class AdminUpdateTeamRequest : DiscordIdRequest
{
    public ulong EventId { get; set; }
    public int TeamId { get; set; }
    [FastEndpoints.FromBody]
    public required UpdateEventTeamDto Team { get; set; }
}

internal sealed class UpdateTeamAdminEndpoint(
	IOutputCacheStore cacheStore,
    IEventTeamService teamService)
	: Endpoint<AdminUpdateTeamRequest>
{
	public override void Configure() {
		Patch("/guild/{DiscordId}/events/{EventId}/teams/{TeamId}");
		Options(o => o.WithMetadata(new GuildAdminAuthorizeAttribute()));
		Version(0);

		Summary(s => {
			s.Summary = "Update a team";
		});
	}

	public override async Task HandleAsync(AdminUpdateTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await Send.UnauthorizedAsync(c);
			return;
		}

		var response = await teamService.UpdateTeamAsync(request.TeamId, request.Team, userId, true);

		if (response is BadRequestObjectResult bad) {
			ThrowError(bad.Value?.ToString() ?? "Failed to update team");
		}
		
		if (request.Team.ChangeCode is true) {
			await teamService.RegenerateJoinCodeAsync(request.TeamId, userId);
		}
		
		await cacheStore.EvictByTagAsync("event-teams", c);
		await Send.NoContentAsync(cancellation: c);
	}
}

internal sealed class UpdateTeamRequestValidator : Validator<AdminUpdateTeamRequest>
{
	public UpdateTeamRequestValidator() {
		Include(new DiscordIdRequestValidator());
	}
}
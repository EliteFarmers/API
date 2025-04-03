using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Events.User.UpdateTeamCode;

internal sealed class UpdateTeamJoinCodeRequest
{
    public ulong EventId { get; set; }
    public int TeamId { get; set; }
}

internal sealed class UpdateTeamJoinCodeEndpoint(
    IEventTeamService teamService)
	: Endpoint<UpdateTeamJoinCodeRequest>
{
	public override void Configure() {
		Post("/event/{EventId}/team/{TeamId}/code");
		Version(0);
		
		Description(s => s.Accepts<UpdateTeamJoinCodeRequest>());

		Summary(s => {
			s.Summary = "Generate new team join code";
		});
	}

	public override async Task HandleAsync(UpdateTeamJoinCodeRequest request, CancellationToken c) {
		var userId = User.GetId();
		if (userId is null) {
			await SendUnauthorizedAsync(c);
			return;
		}

		var response = await teamService.RegenerateJoinCodeAsync(request.TeamId, userId);

		switch (response) {
			case BadRequestObjectResult bad:
				ThrowError(bad.Value?.ToString() ?? "Failed to generate new join code");
				break;
			case UnauthorizedObjectResult:
				await SendUnauthorizedAsync(c);
				return;
		}
		
		await SendNoContentAsync(cancellation: c);
	}
}

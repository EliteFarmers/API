using EliteAPI.Authentication;
using EliteAPI.Features.Auth.Models;
using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Events.Public.GetEventTeam;

internal sealed class GetEventTeamRequest {
	public ulong EventId { get; set; }
	public int TeamId { get; set; }
}

internal sealed class GetEventTeamEndpoint(
	IEventTeamService teamService,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEventTeamRequest, EventTeamWithMembersDto>
{
	public override void Configure() {
		Get("/event/{EventId}/team/{TeamId}");
		Options(o => o.WithMetadata(new OptionalAuthorizeAttribute()));
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get an event team";
		});
	}

	public override async Task HandleAsync(GetEventTeamRequest request, CancellationToken c) {
		var userId = User.GetId();
		var isAdmin = User.IsInRole(ApiUserPolicies.Admin);
		
		var team = await teamService.GetTeamAsync(request.TeamId);
		if (team is null) {
			await Send.NotFoundAsync(c);
			return;
		}
		
		var result = mapper.Map<EventTeamWithMembersDto>(team);
		
		// If the user is the owner of the team, return the join code
		if (userId is not null && (team.UserId == userId || isAdmin)) {
			result.JoinCode = team.JoinCode;
		} else {
			result.JoinCode = null;
		}

		await Send.OkAsync(result, cancellation: c);
	}
}
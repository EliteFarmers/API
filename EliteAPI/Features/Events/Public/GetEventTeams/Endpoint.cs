using EliteAPI.Features.Events.Services;
using EliteAPI.Models.DTOs.Outgoing;
using FastEndpoints;

namespace EliteAPI.Features.Events.Public.GetEventTeams;

internal sealed class GetEventTeamsRequest {
	public ulong EventId { get; set; }
}

internal sealed class GetEventTeamsEndpoint(
	IEventTeamService teamService,
	AutoMapper.IMapper mapper)
	: Endpoint<GetEventTeamsRequest, List<EventTeamWithMembersDto>> {
	public override void Configure() {
		Get("/event/{EventId}/teams");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get event teams"; });

		Options(opt => opt.CacheOutput(o => o.Expire(TimeSpan.FromMinutes(2)).Tag("event-teams")));
	}

	public override async Task HandleAsync(GetEventTeamsRequest request, CancellationToken c) {
		var teams = await teamService.GetEventTeamsAsync(request.EventId);
		var result = mapper.Map<List<EventTeamWithMembersDto>>(teams);

		await Send.OkAsync(result, c);
	}
}
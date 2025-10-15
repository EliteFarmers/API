using EliteAPI.Configuration.Settings;
using EliteAPI.Features.Events.Services;
using EliteAPI.Utilities;
using FastEndpoints;

namespace EliteAPI.Features.Events.Public.GetTeamWordList;

internal sealed class GetTeamWordListEndpoint(IEventTeamService teamService)
	: EndpointWithoutRequest<EventTeamsWordListDto>
{
	public override void Configure() {
		Get("/event/teams/words");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get event team word list constants";
			s.Description = "Lists of whitelisted words for team name generation.";
		});

		Options(opt => opt.CacheOutput(CachePolicy.Hours));
	}

	public override async Task HandleAsync(CancellationToken c) {
		var result = teamService.GetEventTeamNameWords();
		await Send.OkAsync(result, c);
	}
}
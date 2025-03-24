using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerLeaderboardRanks;

internal sealed class LeaderboardRanksResponse {
	public Dictionary<string, PlayerLeaderboardEntryWithRankDto> Ranks { get; set; } = new();
}

internal sealed class GetPlayerLeaderboardRanksEndpoint(
	ILbService lbService,
	IMemberService memberService
	) : Endpoint<PlayerProfileUuidRequest, LeaderboardRanksResponse> 
{
	public override void Configure() {
		Get("/leaderboards/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
		Description(d => d.AutoTagOverride("Leaderboard"));
		Summary(s => {
			s.Summary = "Get a Player's Leaderboard Ranks";
		});
	}

	public override async Task HandleAsync(PlayerProfileUuidRequest request, CancellationToken c) {
		var member = await memberService.GetProfileMemberId(request.PlayerUuidFormatted, request.ProfileUuidFormatted);
		if (member is null) {
			ThrowError("Profile member not found.", StatusCodes.Status404NotFound);
		}
		
		var entries = await lbService.GetPlayerLeaderboardEntriesWithRankAsync(member.Value);
		var profileEntries = await lbService.GetProfileLeaderboardEntriesWithRankAsync(request.ProfileUuidFormatted);

		var response = new LeaderboardRanksResponse {
			Ranks = entries.Concat(profileEntries).ToDictionary(e => e.Slug, e => e)
		};
		
		await SendAsync(response, cancellation: c);
	}
}
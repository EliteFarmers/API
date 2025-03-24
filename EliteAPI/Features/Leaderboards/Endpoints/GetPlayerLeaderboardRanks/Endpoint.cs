using EliteAPI.Features.Leaderboards.Services;
using EliteAPI.Models.Common;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Leaderboards.Endpoints.GetPlayerLeaderboardRanks;

internal sealed class GetPlayerLeaderboardRanksEndpoint(
	ILbService lbService,
	IMemberService memberService
	) : Endpoint<PlayerProfileUuidRequest, List<PlayerLeaderboardEntryWithRankDto>> 
{
	
	public override void Configure() {
		Get("/leaderboards/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);
		
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

		
		await SendAsync(entries.Concat(profileEntries).ToList(), cancellation: c);
	}
}
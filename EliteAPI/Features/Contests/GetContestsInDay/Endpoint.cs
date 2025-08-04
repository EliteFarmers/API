using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Contests.GetContestsInDay;

internal sealed class GetContestsInDayEndpoint(
	IContestsService contestsService)
	: Endpoint<SkyBlockDayRequest, List<JacobContestWithParticipationsDto>> 
{
	public override void Configure() {
		Get("/contests/at/{Year}/{Month}/{Day}");
		AllowAnonymous();
		ResponseCache(600);
		
		Summary(s => {
			s.Summary = "Get all contests in a SkyBlock year";
		});
	}

	public override async Task HandleAsync(SkyBlockDayRequest request, CancellationToken ct) {
		var timestamp = FormatUtils.GetTimeFromSkyblockDate(request.Year - 1, request.Month - 1, request.Day - 1);
		var result = await contestsService.GetContestsAt(timestamp);
		
		await Send.OkAsync(result, cancellation: ct);
	}
}
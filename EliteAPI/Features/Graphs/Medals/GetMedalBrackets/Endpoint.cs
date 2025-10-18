using EliteAPI.Features.Contests;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Graphs.Medals.GetMedalBrackets;

internal sealed class GetMedalBracketsEndpoint(
	IContestsService contestsService)
	: Endpoint<GetMedalBracketsRequest, ContestBracketsDetailsDto>
{
	public override void Configure() {
		Get("/graph/medals/{Year:int}/{Month:int}");
		AllowAnonymous();
		ResponseCache(600);

		Description(s => s.Accepts<GetMedalBracketsRequest>());

		Summary(s => { s.Summary = "Get average medal brackets for a specific SkyBlock month"; });
	}

	public override async Task HandleAsync(GetMedalBracketsRequest request, CancellationToken c) {
		switch (request.Month) {
			case < 1:
				ThrowError("Month cannot be less than 1.");
				break;
			case > 12:
				ThrowError("Month cannot be greater than 12.");
				break;
		}

		switch (request.Months) {
			case < 1:
				ThrowError("Months cannot be less than 1.");
				break;
			case > 12:
				ThrowError("Months cannot be greater than 12.");
				break;
		}

		var start = new SkyblockDate(request.Year - 1, request.Month - (request.Months ?? 2), 0).UnixSeconds;
		var end = new SkyblockDate(request.Year - 1, request.Month, 0).UnixSeconds;

		var brackets = await contestsService.GetAverageMedalBrackets(start, end);

		var result = new ContestBracketsDetailsDto {
			Start = start.ToString(),
			End = end.ToString(),
			Brackets = brackets ?? new Dictionary<string, ContestBracketsDto>()
		};

		await Send.OkAsync(result, c);
	}
}
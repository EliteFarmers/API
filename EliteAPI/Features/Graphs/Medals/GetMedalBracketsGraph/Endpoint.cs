using EliteAPI.Features.Contests;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Graphs.Medals.GetMedalBracketsGraph;

internal sealed class GetMedalBracketsGraphEndpoint(
	IContestsService contestsService)
	: Endpoint<GetMedalBracketsGraphRequest, List<ContestBracketsDetailsDto>> {
	public override void Configure() {
		Get("/graph/medals/{Year:int}");
		AllowAnonymous();
		ResponseCache(600);

		Description(s => s.Accepts<GetMedalBracketsGraphRequest>());

		Summary(s => { s.Summary = "Get average medal brackets for multiple SkyBlock years"; });
	}

	public override async Task HandleAsync(GetMedalBracketsGraphRequest request, CancellationToken c) {
		switch (request.Years) {
			case < 1:
				ThrowError("Years cannot be less than 1.");
				break;
			case > 5:
				ThrowError("Years cannot be greater than 5.");
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

		var result = new List<ContestBracketsDetailsDto>();

		for (var i = request.Years ?? 1; i > 0; i--) {
			for (var month = 0; month < 12; month++) {
				var start = new SkyblockDate(request.Year - (request.Years ?? 1) - 1, month - (request.Months ?? 2), 0)
					.UnixSeconds;
				var end = new SkyblockDate(request.Year - (request.Years ?? 1), month, 0).UnixSeconds;

				result.Add(new ContestBracketsDetailsDto {
					Start = start.ToString(),
					End = end.ToString(),
					Brackets = await contestsService.GetAverageMedalBrackets(start, end) ??
					           new Dictionary<string, ContestBracketsDto>()
				});
			}
		}

		await Send.OkAsync(result, c);
	}
}
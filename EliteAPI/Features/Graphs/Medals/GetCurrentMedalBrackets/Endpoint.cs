using System.ComponentModel;
using EliteAPI.Features.Contests;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Graphs.Medals.GetCurrentMedalBrackets;

internal sealed class GetCurrentMedalBracketsRequest {
	/// <summary>
	/// Amount of previous SkyBlock months to include in the average
	/// </summary>
	[QueryParam]
	[DefaultValue(2)]
	public int? Months { get; set; } = 2;
}

internal sealed class GetCurrentMedalBracketsEndpoint(
	IContestsService contestsService)
	: Endpoint<GetCurrentMedalBracketsRequest, ContestBracketsDetailsDto> {
	public override void Configure() {
		Get("/graph/medals/now");
		AllowAnonymous();
		ResponseCache(600);

		Description(s => s.Accepts<GetCurrentMedalBracketsRequest>());

		Summary(s => { s.Summary = "Get current average medal brackets"; });
	}

	public override async Task HandleAsync(GetCurrentMedalBracketsRequest request, CancellationToken c) {
		switch (request.Months) {
			case < 1:
				ThrowError("Months cannot be less than 1.");
				break;
			case > 12:
				ThrowError("Months cannot be greater than 12.");
				break;
		}

		// Exclude the last 3 hours to minimize the chance of inaccurate data from new contests
		var end = new SkyblockDate(DateTimeOffset.UtcNow.AddHours(-3).ToUnixTimeSeconds());
		var start = new SkyblockDate(end.Year - 1, end.Month - (request.Months ?? 2), end.Day).UnixSeconds;

		var brackets = await contestsService.GetAverageMedalBrackets(start, end.UnixSeconds);

		var result = new ContestBracketsDetailsDto {
			Start = start.ToString(),
			End = end.UnixSeconds.ToString(),
			Brackets = brackets ?? new Dictionary<string, ContestBracketsDto>()
		};

		await Send.OkAsync(result, c);
	}
}
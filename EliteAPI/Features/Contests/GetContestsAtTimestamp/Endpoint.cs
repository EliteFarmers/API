using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;

namespace EliteAPI.Features.Contests.GetContestsAtTimestamp;

public class GetContestsAtTimestampRequest
{
	public long Timestamp { get; set; }

	/// <summary>
	/// Limit the number of participations returned in each contest.
	/// </summary>
	[QueryParam]
	public int Limit { get; set; } = -1;
}

internal sealed class GetContestsAtTimestampEndpoint(
	IContestsService contestsService)
	: Endpoint<GetContestsAtTimestampRequest, List<JacobContestWithParticipationsDto>>
{
	public override void Configure() {
		Get("/contests/{Timestamp:long}");
		AllowAnonymous();
		ResponseCache(600, varyByQueryKeys: ["limit"]);

		Summary(s => { s.Summary = "Get the three contests that start at a specific timestamp"; });
	}

	public override async Task HandleAsync(GetContestsAtTimestampRequest request, CancellationToken ct) {
		var skyblockDate = new SkyblockDate(request.Timestamp);
		if (!skyblockDate.IsValid()) ThrowError("Invalid timestamp");

		var result = await contestsService.GetContestsAt(skyblockDate.StartOfDayTimestamp(), request.Limit);

		await Send.OkAsync(result, ct);
	}
}
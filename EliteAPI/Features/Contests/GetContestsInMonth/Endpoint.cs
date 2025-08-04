using EliteAPI.Data;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Contests.GetContestsInMonth;

internal sealed class GetContestsInMonthEndpoint(
	AutoMapper.IMapper mapper,
	DataContext context)
	: Endpoint<SkyBlockMonthRequest, Dictionary<int, List<JacobContestDto>>> 
{
	public override void Configure() {
		Get("/contests/at/{Year}/{Month}");
		AllowAnonymous();
		ResponseCache(600);
		
		Summary(s => {
			s.Summary = "Get all contests in a SkyBlock month";
		});
	}

	public override async Task HandleAsync(SkyBlockMonthRequest request, CancellationToken ct) {
		var startTime = FormatUtils.GetTimeFromSkyblockDate(request.Year - 1, request.Month - 1, 0);
		var endTime = FormatUtils.GetTimeFromSkyblockDate(request.Year - 1, request.Month, 0);

		var contests = await context.JacobContests
			.Where(j => j.Timestamp >= startTime && j.Timestamp < endTime)
			.ToListAsync(cancellationToken: ct);

		var mappedContests = mapper.Map<List<JacobContestDto>>(contests);

		var data = new Dictionary<int, List<JacobContestDto>>();

		foreach (var contest in mappedContests) {
			var skyblockDate = new SkyblockDate(contest.Timestamp);
			var day = skyblockDate.Day + 1;

			if (data.TryGetValue(day, out var value))
			{
				value.Add(contest);
			}
			else
			{
				data.Add(day, [contest]);
			}
		}
		
		await Send.OkAsync(data, cancellation: ct);
	}
}
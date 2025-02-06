using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using CacheControlHeaderValue = Microsoft.Net.Http.Headers.CacheControlHeaderValue;

namespace EliteAPI.Features.Contests.GetContestsInYear;

internal sealed class GetContestsInYearEndpoint(
	IContestsService contestsService)
	: Endpoint<GetContestsInYearRequest, YearlyContestsDto> 
{
	public override void Configure() {
		Get("/contests/at/{Year:int}");
		AllowAnonymous();
		ResponseCache(600);
		
		Summary(s => {
			s.Summary = "Get all contests in a SkyBlock year";
		});
	}

	public override async Task HandleAsync(GetContestsInYearRequest request, CancellationToken ct) {
		// Decrease cache time to 2 minutes if it's the end/start of the year in preparation for the next year
		if (request.Now is true && SkyblockDate.Now is { Month: >= 11, Day: >= 27 } or { Month: 0, Day: <= 2 }) {
			HttpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
				Public = true,
				MaxAge = TimeSpan.FromMinutes(2)
			};
		}

		var result = await contestsService.GetContestsFromYear(request.Year);
		
		await SendAsync(result, cancellation: ct);
	}
}
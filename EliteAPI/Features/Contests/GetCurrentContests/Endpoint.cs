using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using CacheControlHeaderValue = Microsoft.Net.Http.Headers.CacheControlHeaderValue;

namespace EliteAPI.Features.Contests.GetCurrentContests;

internal sealed class GetCurrentContestsEndpoint(
	IContestsService contestsService)
	: EndpointWithoutRequest<YearlyContestsDto> 
{
	public override void Configure() {
		Get("/contests/at/now");
		AllowAnonymous();
		ResponseCache(600);
		
		Summary(s => {
			s.Summary = "Get upcoming contests for the current SkyBlock year";
			s.Description = "Uses crowd-sourced data, which may not be accurate.\nData used and provided by <see href=\"https://github.com/hannibal002/SkyHanni/\">SkyHanni</see> to display upcoming contests in-game.";
		});
	}

	public override async Task HandleAsync(CancellationToken ct) {
		// Decrease cache time to 2 minutes if it's the end/start of the year in preparation for the next year
		if (SkyblockDate.Now is { Month: >= 11, Day: >= 27 } or { Month: 0, Day: <= 2 }) {
			HttpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue {
				Public = true,
				MaxAge = TimeSpan.FromMinutes(2)
			};
		}

		var result = await contestsService.GetContestsFromYear(SkyblockDate.Now.Year + 1, true);
		
		await Send.OkAsync(result, cancellation: ct);
	}
}
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Utilities;
using FastEndpoints.Swagger;

namespace EliteAPI.Features.Contests.GetContestFromKey;

public class GetContestFromKeyRequest {
	/// <summary>
	/// A contest key in the format from the Hypixel API
	/// </summary>
	public required string ContestKey { get; set; }
}

internal sealed class GetContestFromKeyEndpoint(
	IContestsService contestsService)
	: Endpoint<GetContestFromKeyRequest, JacobContestWithParticipationsDto> 
{
	public override void Configure() {
		Get("/contest/{ContestKey}");
		AllowAnonymous();
		ResponseCache(600);
		
		Description(d => d.AutoTagOverride("Contests"));
		
		Summary(s => {
			s.Summary = "Get a contest from a contest key";
			s.ExampleRequest = new GetContestFromKeyRequest() {
				ContestKey = "285:2_11:CACTUS"
			};
		});
	}

	public override async Task HandleAsync(GetContestFromKeyRequest request, CancellationToken ct) {
		var time = FormatUtils.GetTimeFromContestKey(request.ContestKey);
		var crop = FormatUtils.GetCropFromContestKey(request.ContestKey);
		
		if (time == 0) {
			AddError("Invalid timestamp");
		}
		
		if (crop is null) {
			AddError("Invalid crop");
		}
		
		ThrowIfAnyErrors();

		var result = await contestsService.GetContestFromKey(request.ContestKey);
		
		if (result is null) {
			ThrowError("Contest not found");
		}
		
		await SendAsync(result, cancellation: ct);
	}
}
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Garden.GetSelectedGarden;

using Result = Results<Ok<GardenDto>, NotFound>;

internal sealed class GetSelectedGardenEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper)
	: Endpoint<PlayerUuidRequest, Result>
{
	public override void Configure() {
		Get("/garden/{PlayerUuid}/selected");
		AllowAnonymous();
		ResponseCache(600, ResponseCacheLocation.Any);

		Summary(s => {
			s.Summary = "Get selected Garden data for a player";
			s.Description = "Get selected Garden data for a specific player by UUID";
			s.ExampleRequest = new PlayerUuidRequest {
				PlayerUuid = "7da0c47581dc42b4962118f8049147b7"
			};
		});
	}

	public override async Task<Result> ExecuteAsync(PlayerUuidRequest request, CancellationToken c) {
		var garden = await profileService.GetSelectedGarden(request.PlayerUuidFormatted);
		if (garden is null) return TypedResults.NotFound();

		var mapped = mapper.Map<GardenDto>(garden);
		return TypedResults.Ok(mapped);
	}
}
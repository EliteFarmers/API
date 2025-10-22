using EliteAPI.Models.Common;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using IMapper = AutoMapper.IMapper;

namespace EliteAPI.Features.Garden.GetGarden;

using Result = Results<Ok<GardenDto>, NotFound>;

internal sealed class GetGardenEndpoint(
	IProfileService profileService,
	IMapper mapper)
	: Endpoint<ProfileUuidRequest, Result>
{
	public override void Configure() {
		Get("/garden/{ProfileUuid}");
		AllowAnonymous();
		ResponseCache(600, ResponseCacheLocation.Any);

		Summary(s => {
			s.Summary = "Get Garden data for a profile";
			s.Description = "Get Garden data for a specific profile by UUID";
			s.ExampleRequest = new ProfileUuidRequest {
				ProfileUuid = "7da0c47581dc42b4962118f8049147b7"
			};
		});
	}

	public override async Task<Result> ExecuteAsync(ProfileUuidRequest request, CancellationToken c) {
		var garden = await profileService.GetProfileGarden(request.ProfileUuidFormatted);
		if (garden is null) return TypedResults.NotFound();

		var mapped = mapper.Map<GardenDto>(garden);
		return TypedResults.Ok(mapped);
	}
}
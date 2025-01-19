using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using IMapper = AutoMapper.IMapper;

namespace EliteAPI.Features.Garden.GetGarden;

using Result = Results<Ok<GardenDto>, NotFound>;

internal sealed class GetGardenEndpoint(
	IProfileService profileService,
	IMapper mapper)
	: Endpoint<GetGardenRequest, Result> 
{
	public override void Configure() {
		Get("/garden/{ProfileUuid}");
		AllowAnonymous();
		ResponseCache(600, ResponseCacheLocation.Any);
		
		Summary(s => {
			s.Summary = "Get Garden data for a profile";
			s.Description = "Get Garden data for a specific profile by UUID";
			s.ExampleRequest = new GetGardenRequest {
				ProfileUuid = "7da0c47581dc42b4962118f8049147b7"
			};
		});
	}

	public override async Task<Result> ExecuteAsync(GetGardenRequest request, CancellationToken c) {
		var garden = await profileService.GetProfileGarden(request.ProfileUuid);
		if (garden is null) {
			return TypedResults.NotFound();
		}
		
		var mapped = mapper.Map<GardenDto>(garden);
		return TypedResults.Ok(mapped);
	}
}

internal sealed class GetGardenRequest {
	public required string ProfileUuid { get; set; }
}

internal sealed class GetGardenValidator : Validator<GetGardenRequest> {
	public GetGardenValidator() {
		RuleFor(x => x.ProfileUuid)
			.NotEmpty()
			.WithMessage("ProfileUuid is required")
			.Matches("^[a-fA-F0-9]{32}$")
			.WithMessage("ProfileUuid must match ^[a-fA-F0-9]{32}$");
	}
}

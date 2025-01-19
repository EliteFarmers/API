using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EliteAPI.Features.Garden.GetSelectedGarden;

using Result = Results<Ok<GardenDto>, NotFound>;

internal sealed class GetGardenEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper)
	: Endpoint<GetSelectedGardenRequest, Result> 
{
	public override void Configure() {
		Get("/garden/{PlayerUuid}/selected");
		AllowAnonymous();
		ResponseCache(600, ResponseCacheLocation.Any);
		
		Summary(s => {
			s.Summary = "Get selected Garden data for a player";
			s.Description = "Get selected Garden data for a specific player by UUID";
			s.ExampleRequest = new GetSelectedGardenRequest {
				PlayerUuid = "7da0c47581dc42b4962118f8049147b7"
			};
		});
	}

	public override async Task<Result> ExecuteAsync(GetSelectedGardenRequest request, CancellationToken c) {
		var garden = await profileService.GetSelectedGarden(request.PlayerUuid);
		if (garden is null) {
			return TypedResults.NotFound();
		}
		
		var mapped = mapper.Map<GardenDto>(garden);
		return TypedResults.Ok(mapped);
	}
}

internal sealed class GetSelectedGardenRequest {
	public required string PlayerUuid { get; set; }
}

internal sealed class GetGardenValidator : Validator<GetSelectedGardenRequest> {
	public GetGardenValidator() {
		RuleFor(x => x.PlayerUuid)
			.NotEmpty()
			.WithMessage("ProfileUuid is required")
			.Matches("^[a-fA-F0-9]{32}$")
			.WithMessage("ProfileUuid must match ^[a-fA-F0-9]{32}$");
	}
}

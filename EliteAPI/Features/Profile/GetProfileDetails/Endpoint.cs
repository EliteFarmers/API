using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EliteAPI.Features.Profile.GetProfileDetails;

using Result = Results<Ok<ProfileDetailsDto>, NotFound>;

internal sealed class GetProfileDetailsEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<PlayerUuidRequest, Result> {
	
	public override void Configure() {
		Get("/profile/{PlayerUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Profile Details";
		});
	}

	public override async Task<Result> ExecuteAsync(PlayerUuidRequest request, CancellationToken c) {
		var member = await profileService.GetProfile(request.PlayerUuidFormatted);
		if (member is null) return TypedResults.NotFound();

		var mapped = mapper.Map<ProfileDetailsDto>(member);
		return TypedResults.Ok(mapped);
	}
}
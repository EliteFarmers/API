using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EliteAPI.Features.Profiles.Endpoints.GetProfileDetails;

using Result = Results<Ok<ProfileDetailsDto>, NotFound>;

internal sealed class GetProfileDetailsEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<ProfileUuidRequest, Result>
{
	public override void Configure() {
		Get("/profile/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => { s.Summary = "Get Profile Details"; });
	}

	public override async Task<Result> ExecuteAsync(ProfileUuidRequest request, CancellationToken c) {
		var member = await profileService.GetProfile(request.ProfileUuidFormatted);
		if (member is null) return TypedResults.NotFound();

		var mapped = mapper.Map<ProfileDetailsDto>(member);
		return TypedResults.Ok(mapped);
	}
}
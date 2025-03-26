using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EliteAPI.Features.Profiles.Endpoints.GetProfile;

using Result = Results<Ok<ProfileMemberDto>, NotFound>;

internal sealed class GetProfileEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<PlayerProfileUuidRequest, Result> {
	
	public override void Configure() {
		Get("/profile/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Profile Member";
		});
	}

	public override async Task<Result> ExecuteAsync(PlayerProfileUuidRequest request, CancellationToken c) {
		var member = await profileService.GetProfileMember(request.PlayerUuidFormatted, request.ProfileUuidFormatted);
		if (member is null) return TypedResults.NotFound();

		var mapped = mapper.Map<ProfileMemberDto>(member);
		return TypedResults.Ok(mapped);
	}
}
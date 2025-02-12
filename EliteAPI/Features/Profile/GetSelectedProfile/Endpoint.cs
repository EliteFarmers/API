using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EliteAPI.Features.Profile.GetSelectedProfile;

using Result = Results<Ok<ProfileMemberDto>, NotFound>;

internal sealed class GetSelectedProfileEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<PlayerUuidRequest, Result> {
	
	public override void Configure() {
		Get("/profile/{PlayerUuid}/selected");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Profile Member";
		});
	}

	public override async Task<Result> ExecuteAsync(PlayerUuidRequest request, CancellationToken c) {
		var member = await profileService.GetSelectedProfileMember(request.PlayerUuidFormatted);
		if (member is null) return TypedResults.NotFound();

		var mapped = mapper.Map<ProfileMemberDto>(member);
		return TypedResults.Ok(mapped);
	}
}
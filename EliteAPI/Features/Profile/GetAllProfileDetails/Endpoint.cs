using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;

namespace EliteAPI.Features.Profile.GetAllProfileDetails;

internal sealed class GetAllProfileDetailsEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<PlayerUuidRequest, List<ProfileDetailsDto>> {
	
	public override void Configure() {
		Get("/profiles/{PlayerUuid}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get All Profile Details";
		});
	}

	public override async Task HandleAsync(PlayerUuidRequest request, CancellationToken c) {
		var member = await profileService.GetProfilesDetails(request.PlayerUuidFormatted);
		await SendAsync(member, cancellation: c);
	}
}
using EliteAPI.Data;
using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Profile.GetProfileNames;

internal sealed class GetProfileNamesEndpoint(
	DataContext context,
	IProfileService profileService
) : Endpoint<PlayerRequest, List<ProfileNamesDto>> {
	
	public override void Configure() {
		Get("/profiles/{Player}/names");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get names of a player's profiles";
		});
		
		Description(d => d.AutoTagOverride("Profile"));
	}

	public override async Task HandleAsync(PlayerRequest request, CancellationToken c) {
		var player = await profileService.GetPlayerDataByUuidOrIgn(request.Player);
		if (player is null) {
			await SendAsync([], cancellation: c);
			return;
		}
		
		var profiles = await context.ProfileMembers
			.AsNoTracking()
			.Where(m => m.PlayerUuid.Equals(player.Uuid) && !m.WasRemoved)
			.Select(m => new ProfileNamesDto {
				Id = m.ProfileId,
				Name = m.ProfileName ?? m.Profile.ProfileName,
				Selected = m.IsSelected
			}).ToListAsync(cancellationToken: c);

		await SendAsync(profiles, cancellation: c);
	}
}
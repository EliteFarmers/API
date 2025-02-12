using EliteAPI.Models.Common;
using EliteAPI.Models.DTOs.Outgoing;
using EliteAPI.Services.Interfaces;
using FastEndpoints;
using Microsoft.AspNetCore.Http.HttpResults;

namespace EliteAPI.Features.Player.GetPlayerData;

using Result = Results<Ok<PlayerDataDto>, NotFound>;

internal sealed class GetPlayerDataEndpoint(
	IProfileService profileService,
	AutoMapper.IMapper mapper
) : Endpoint<PlayerRequest, Result> {
	
	public override void Configure() {
		Get("/player/{Player}");
		AllowAnonymous();
		Version(0);

		Summary(s => {
			s.Summary = "Get Player Data";
		});
	}

	public override async Task<Result> ExecuteAsync(PlayerRequest request, CancellationToken c) {
		var player = await profileService.GetPlayerDataByUuidOrIgn(request.Player);
		if (player is null) {
			return TypedResults.NotFound();
		}
		return TypedResults.Ok(mapper.Map<PlayerDataDto>(player));
	}
}
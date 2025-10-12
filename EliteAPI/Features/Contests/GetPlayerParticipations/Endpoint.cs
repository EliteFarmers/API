using EliteAPI.Data;
using EliteAPI.Models.Common;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Contests.GetPlayerParticipations;

internal sealed class GetPlayerParticipationsEndpoint(
	AutoMapper.IMapper mapper,
	DataContext context)
	: Endpoint<PlayerUuidRequest, List<ContestParticipationDto>> {
	public override void Configure() {
		Get("/contests/{PlayerUuid}");
		AllowAnonymous();
		ResponseCache(600);

		Summary(s => { s.Summary = "Get all contests for a player"; });
	}

	public override async Task HandleAsync(PlayerUuidRequest request, CancellationToken ct) {
		var profileMembers = await context.ProfileMembers
			.Where(p => p.PlayerUuid.Equals(request.PlayerUuidFormatted))
			.Include(p => p.JacobData)
			.ThenInclude(j => j.Contests)
			.ThenInclude(c => c.JacobContest)
			.AsSplitQuery()
			.ToListAsync(ct);

		if (profileMembers.Count == 0) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var data = new List<ContestParticipationDto>();

		foreach (var profileMember in profileMembers) {
			data.AddRange(mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests));
		}

		await Send.OkAsync(data, ct);
	}
}
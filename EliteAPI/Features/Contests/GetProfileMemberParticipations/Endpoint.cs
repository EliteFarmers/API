using EliteAPI.Data;
using EliteAPI.Models.Common;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Contests.GetProfileMemberParticipations;

internal sealed class GetProfileMemberParticipationsEndpoint(
	AutoMapper.IMapper mapper,
	DataContext context)
	: Endpoint<PlayerProfileUuidRequest, List<ContestParticipationDto>>
{
	public override void Configure() {
		Get("/contests/{PlayerUuid}/{ProfileUuid}");
		AllowAnonymous();
		ResponseCache(600);

		Summary(s => { s.Summary = "Get all contests for a profile member"; });
	}

	public override async Task HandleAsync(PlayerProfileUuidRequest request, CancellationToken ct) {
		var profileMember = await context.ProfileMembers
			.Where(p => p.PlayerUuid.Equals(request.PlayerUuidFormatted) &&
			            p.ProfileId.Equals(request.ProfileUuidFormatted))
			.Include(p => p.JacobData)
			.ThenInclude(j => j.Contests)
			.ThenInclude(c => c.JacobContest)
			.AsSplitQuery()
			.FirstOrDefaultAsync(ct);

		if (profileMember is null) {
			await Send.NotFoundAsync(ct);
			return;
		}

		var data = mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests);

		await Send.OkAsync(data, ct);
	}
}
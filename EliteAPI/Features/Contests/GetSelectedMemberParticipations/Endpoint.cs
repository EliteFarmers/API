using EliteAPI.Data;
using EliteAPI.Models.Common;
using FastEndpoints;
using EliteAPI.Models.DTOs.Outgoing;
using Microsoft.EntityFrameworkCore;

namespace EliteAPI.Features.Contests.GetSelectedMemberParticipations;

internal sealed class GetSelectedMemberParticipationsEndpoint(
	AutoMapper.IMapper mapper,
	DataContext context)
	: Endpoint<PlayerUuidRequest, List<ContestParticipationDto>> 
{
	public override void Configure() {
		Get("/contests/{PlayerUuid}/selected");
		AllowAnonymous();
		ResponseCache(600);
		
		Summary(s => {
			s.Summary = "Get contests for the player's selected profile member";
		});
	}

	public override async Task HandleAsync(PlayerUuidRequest request, CancellationToken ct) {
		var profileMember = await context.ProfileMembers
			.Where(p => p.PlayerUuid.Equals(request.PlayerUuidFormatted) && p.IsSelected)
			.Include(p => p.JacobData)
			.ThenInclude(j => j.Contests)
			.ThenInclude(c => c.JacobContest)
			.AsSplitQuery()
			.FirstOrDefaultAsync(cancellationToken: ct);
		
		if (profileMember is null) {
			await SendNotFoundAsync(ct);
			return;
		}

		var data = mapper.Map<List<ContestParticipationDto>>(profileMember.JacobData.Contests);
		
		await SendAsync(data, cancellation: ct);
	}
}